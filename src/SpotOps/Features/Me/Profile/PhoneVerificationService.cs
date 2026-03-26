using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SpotOps.Data;
using SpotOps.Infrastructure.Sms;

namespace SpotOps.Features.Me.Profile;

public sealed class PhoneVerificationService
{
    private sealed record OtpSession(string Phone, string Code, DateTime ExpiresAtUtc, int FailedAttempts);

    private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan SendCooldown = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan SendWindow = TimeSpan.FromHours(1);
    private const int MaxSendsPerWindow = 5;
    private const int MaxAttempts = 5;
    private static readonly ConcurrentDictionary<Guid, OtpSession> _sessions = new();
    private static readonly ConcurrentDictionary<Guid, List<DateTime>> _sendHistories = new();

    private readonly AppDbContext _db;
    private readonly ISmsSender _smsSender;
    private readonly ILogger<PhoneVerificationService> _logger;

    public PhoneVerificationService(AppDbContext db, ISmsSender smsSender, ILogger<PhoneVerificationService> logger)
    {
        _db = db;
        _smsSender = smsSender;
        _logger = logger;
    }

    public async Task<(bool Ok, string? ErrorCode, string? ErrorMessage)> SendOtpAsync(
        Guid userId,
        string phone,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizePhone(phone);
        if (normalized.Length < 8 || normalized.Length > 20)
            return (false, "PHONE_INVALID_FORMAT", "전화번호 형식이 올바르지 않아요.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return (false, "ME_PROFILE_NOT_FOUND", "사용자 정보를 찾을 수 없어요.");

        var throttle = CheckAndTrackSendRate(userId, DateTime.UtcNow);
        if (!throttle.Ok)
            return (false, "PHONE_OTP_RATE_LIMITED", $"요청이 너무 많아요. {throttle.RetryAfterSec}초 후 다시 시도해주세요.");

        if (!string.Equals(user.Phone, normalized, StringComparison.Ordinal))
        {
            user.Phone = normalized;
            user.PhoneVerifiedAt = null;
            await _db.SaveChangesAsync(cancellationToken);
        }

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var session = new OtpSession(normalized, code, DateTime.UtcNow.Add(OtpTtl), 0);
        _sessions.AddOrUpdate(userId, session, (_, _) => session);

        await _smsSender.SendAsync(normalized, $"[SpotOps] 인증번호는 {code} 입니다.", cancellationToken);
        _logger.LogInformation("Phone OTP issued for user {UserId}", userId);

        return (true, null, null);
    }

    public async Task<(bool Ok, string? ErrorCode, string? ErrorMessage)> VerifyOtpAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryGetValue(userId, out var session))
            return (false, "PHONE_OTP_NOT_FOUND", "먼저 인증번호를 요청해주세요.");

        if (DateTime.UtcNow > session.ExpiresAtUtc)
        {
            _sessions.TryRemove(userId, out _);
            return (false, "PHONE_OTP_EXPIRED", "인증번호가 만료되었어요.");
        }

        var trimmedCode = (code ?? string.Empty).Trim();
        if (!string.Equals(session.Code, trimmedCode, StringComparison.Ordinal))
        {
            var nextAttempts = session.FailedAttempts + 1;
            if (nextAttempts >= MaxAttempts)
            {
                _sessions.TryRemove(userId, out _);
                return (false, "PHONE_OTP_TOO_MANY_ATTEMPTS", "인증 시도 횟수를 초과했어요.");
            }

            _sessions[userId] = session with { FailedAttempts = nextAttempts };
            return (false, "PHONE_OTP_MISMATCH", "인증번호가 일치하지 않아요.");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return (false, "ME_PROFILE_NOT_FOUND", "사용자 정보를 찾을 수 없어요.");

        if (!string.Equals(user.Phone, session.Phone, StringComparison.Ordinal))
            user.Phone = session.Phone;

        user.PhoneVerifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        _sessions.TryRemove(userId, out _);
        return (true, null, null);
    }

    private static string NormalizePhone(string phone)
    {
        return new string((phone ?? string.Empty)
            .Where(ch => char.IsDigit(ch) || ch == '+')
            .ToArray());
    }

    private static (bool Ok, int RetryAfterSec) CheckAndTrackSendRate(Guid userId, DateTime nowUtc)
    {
        var history = _sendHistories.GetOrAdd(userId, _ => []);
        lock (history)
        {
            history.RemoveAll(t => nowUtc - t > SendWindow);

            if (history.Count > 0)
            {
                var elapsed = nowUtc - history[^1];
                if (elapsed < SendCooldown)
                    return (false, Math.Max(1, (int)Math.Ceiling((SendCooldown - elapsed).TotalSeconds)));
            }

            if (history.Count >= MaxSendsPerWindow)
            {
                var oldest = history[0];
                var retry = SendWindow - (nowUtc - oldest);
                return (false, Math.Max(1, (int)Math.Ceiling(retry.TotalSeconds)));
            }

            history.Add(nowUtc);
            return (true, 0);
        }
    }
}

