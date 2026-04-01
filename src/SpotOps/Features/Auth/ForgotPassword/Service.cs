using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SpotOps.Data;
using SpotOps.Infrastructure.Email;
using SpotOps.Models;

namespace SpotOps.Features.Auth.ForgotPassword;

public sealed partial class ForgotPasswordService : IForgotPasswordService
{
    private static readonly TimeSpan PasswordResetTokenTtl = TimeSpan.FromMinutes(30);

    private readonly AppDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ForgotPasswordService> _logger;
    private readonly string _appBaseUrl;

    public ForgotPasswordService(
        AppDbContext db,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<ForgotPasswordService> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _appBaseUrl = (configuration["APP_URL"] ?? string.Empty).Trim().TrimEnd('/');
        _logger = logger;
    }

    /// <summary>
    /// 비밀번호 재설정 이메일 전송
    /// </summary>
    /// <param name="request">비밀번호 재설정 요청 정보</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>유저 정보</returns>
    public async Task<(bool Success, string? ErrorCode)> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = (request.Email ?? string.Empty).Trim().ToLowerInvariant();

        // 유저 존재 여부 추측을 하지 못하게 성공으로 처리
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);
        if (user is null)
            return (true, null);

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var tokenHash = HashPasswordResetToken(token);
        var now = DateTime.UtcNow;

        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = now.Add(PasswordResetTokenTtl),
            CreatedAt = now
        });
        await _db.SaveChangesAsync(ct);

        var encodedToken = Uri.EscapeDataString(token);
        var resetLink = $"{_appBaseUrl}/auth/password-reset?token={encodedToken}";
        var subject = "[SpotOps] 비밀번호 재설정 안내";
        var body = $"비밀번호 재설정 링크: {resetLink}\n만료: {PasswordResetTokenTtl.TotalMinutes}분";
        try
        {
            await _emailSender.SendAsync(user.Email, subject, body, ct);
        }
        catch
        {
            _logger.LogWarning("Forgot password email send failed for user {UserId}", user.Id);
        }

        return (true, null);
    }

    private static string HashPasswordResetToken(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
