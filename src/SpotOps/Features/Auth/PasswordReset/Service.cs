using System.Security.Cryptography;
using System.Text;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using SpotOps.Data;
using SpotOps.Infrastructure.Email;
using SpotOps.Models;

namespace SpotOps.Features.Auth.PasswordReset;

public sealed class PasswordResetService
{
    private static readonly TimeSpan TokenTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan RequestWindow = TimeSpan.FromHours(1);
    private const int MaxRequestsPerWindow = 5;
    private static readonly ConcurrentDictionary<string, List<DateTime>> _requestHistories = new();

    private readonly AppDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(AppDbContext db, IEmailSender emailSender, ILogger<PasswordResetService> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<(bool Ok, string? ErrorCode, string? ErrorMessage)> RequestAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalized = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
            return (true, null, null);

        var throttle = CheckAndTrackRequestRate(normalized, DateTime.UtcNow);
        if (!throttle.Ok)
            return (false, "PASSWORD_RESET_RATE_LIMITED", $"요청이 너무 많아요. {throttle.RetryAfterSec}초 후 다시 시도해주세요.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);
        if (user is null)
            return (true, null, null);

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Hash(rawToken);

        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.Add(TokenTtl)
        });
        await _db.SaveChangesAsync(cancellationToken);

        var subject = "[SpotOps] 비밀번호 재설정 안내";
        var body = $"비밀번호 재설정 토큰: {rawToken}\n만료: {TokenTtl.TotalMinutes}분";

        try
        {
            await _emailSender.SendAsync(user.Email, subject, body, cancellationToken);
        }
        catch
        {
            // 보안상 API 응답은 성공으로 유지. 운영에서는 로그로만 추적.
            _logger.LogWarning("Password reset email sending failed for user {UserId}", user.Id);
        }

        return (true, null, null);
    }

    public async Task<(bool Ok, string? ErrorCode, string? ErrorMessage)> ResetAsync(
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var trimmedToken = (token ?? string.Empty).Trim();
        if (trimmedToken.Length < 16)
            return (false, "PASSWORD_RESET_TOKEN_INVALID", "토큰이 올바르지 않아요.");

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            return (false, "PASSWORD_RESET_PASSWORD_WEAK", "비밀번호는 8자 이상이어야 해요.");

        var tokenHash = Hash(trimmedToken);
        var now = DateTime.UtcNow;

        var resetToken = await _db.PasswordResetTokens
            .Include(t => t.User)
            .Where(t => t.TokenHash == tokenHash)
            .FirstOrDefaultAsync(cancellationToken);

        if (resetToken is null || resetToken.UsedAt is not null || resetToken.ExpiresAt < now)
            return (false, "PASSWORD_RESET_TOKEN_EXPIRED", "토큰이 만료되었거나 이미 사용되었어요.");

        resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        resetToken.UsedAt = now;
        await _db.SaveChangesAsync(cancellationToken);

        return (true, null, null);
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private static (bool Ok, int RetryAfterSec) CheckAndTrackRequestRate(string key, DateTime nowUtc)
    {
        var history = _requestHistories.GetOrAdd(key, _ => []);
        lock (history)
        {
            history.RemoveAll(t => nowUtc - t > RequestWindow);

            if (history.Count >= MaxRequestsPerWindow)
            {
                var oldest = history[0];
                var retry = RequestWindow - (nowUtc - oldest);
                return (false, Math.Max(1, (int)Math.Ceiling(retry.TotalSeconds)));
            }

            history.Add(nowUtc);
            return (true, 0);
        }
    }
}

