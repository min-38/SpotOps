using Microsoft.EntityFrameworkCore;
using SpotOps.Data;
using SpotOps.Features.Auth.JWT;

namespace SpotOps.Features.Auth.Logout;

public sealed class LogoutService : ILogoutService
{
    private readonly AppDbContext _db;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<LogoutService> _logger;

    public LogoutService(AppDbContext db, IJwtTokenService jwtTokenService, ILogger<LogoutService> logger)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    /// <summary>
    /// refresh token 취소
    /// </summary>
    /// <param name="userId">유저 ID</param>
    /// <param name="refreshToken">refresh token</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>void</returns>
    public async Task RevokeRefreshTokenAsync(Guid userId, string refreshToken, CancellationToken ct = default)
    {
        var tokenHash = _jwtTokenService.HashRefresh(refreshToken);
        var existing = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.TokenHash == tokenHash)
            .FirstOrDefaultAsync(ct);

        if (existing is null || existing.RevokedAt is not null)
        {
            _logger.LogWarning("Invalid refresh token: {RefreshToken}", refreshToken);
            throw new InvalidOperationException("Invalid refresh token");
        }

        existing.RevokedAt = DateTime.UtcNow;
        _logger.LogInformation("Refresh token revoked for user {UserId}: {RefreshToken}", userId, refreshToken);
        await _db.SaveChangesAsync(ct);
    }
}
