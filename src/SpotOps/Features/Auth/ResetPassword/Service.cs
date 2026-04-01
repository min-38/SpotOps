using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SpotOps.Data;

namespace SpotOps.Features.Auth.ResetPassword;

public sealed class ResetPasswordService : IResetPasswordService
{
    private readonly AppDbContext _db;

    public ResetPasswordService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 비밀번호 재설정 토큰 검증
    /// </summary>
    /// <param name="token">비밀번호 재설정 토큰</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>토큰 검증 결과</returns>
    public async Task<(bool Ok, string? ErrorCode)> ValidateResetTokenAsync(
        string token,
        CancellationToken ct = default)
    {
        var tokenHash = HashPasswordResetToken(token);
        var now = DateTime.UtcNow;
        var resetToken = await _db.PasswordResetTokens
            .AsNoTracking()
            .Where(t => t.TokenHash == tokenHash)
            .FirstOrDefaultAsync(ct);

        if (resetToken is null || resetToken.UsedAt is not null || resetToken.RevokedAt is not null || resetToken.ExpiresAt <= now)
            return (false, "PASSWORD_RESET_TOKEN_EXPIRED");

        return (true, null);
    }

    /// <summary>
    /// 비밀번호 재설정
    /// </summary>
    /// <param name="request">비밀번호 재설정 요청</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>비밀번호 재설정 결과</returns>
    public async Task<(bool Ok, string? ErrorCode)> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken ct = default)
    {
        // 토큰 해시 생성
        var tokenHash = HashPasswordResetToken(request.Token);
        
        // 토큰 존재 여부 조회
        var resetToken = await _db.PasswordResetTokens
            .Include(t => t.User)
            .Where(t => t.TokenHash == tokenHash)
            .FirstOrDefaultAsync(ct);

        if (resetToken is null || resetToken.UsedAt is not null || resetToken.RevokedAt is not null || resetToken.ExpiresAt <= DateTime.UtcNow)
            return (false, "PASSWORD_RESET_TOKEN_EXPIRED");

        // 비밀번호 재설정
        var now = DateTime.UtcNow;
        resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        resetToken.User.UpdatedAt = now;
        resetToken.UsedAt = now;
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    private static string HashPasswordResetToken(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
