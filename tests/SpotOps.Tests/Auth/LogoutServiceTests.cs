using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SpotOps.Features.Auth.Logout;
using SpotOps.Models;

namespace SpotOps.Tests.Auth;

public class LogoutServiceTests
{
    // 성공
    // 토큰이 유효한 경우
    [Fact]
    public async Task RevokeRefreshTokenAsync_SetsRevokedAt_WhenTokenValid()
    {
        // Arrange
        await using var db = AuthTestDb.CreateContext();
        var userId = Guid.NewGuid();
        var rawToken = "refresh_token_value";
        var tokenHash = "hashed_" + rawToken;

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        });
        await db.SaveChangesAsync();

        // Act
        var service = new LogoutService(db, new StubJwtTokenService(tokenHash), NullLogger<LogoutService>.Instance);
        await service.RevokeRefreshTokenAsync(userId, rawToken);

        // Assert
        var saved = await db.RefreshTokens.FirstAsync();
        Assert.NotNull(saved.RevokedAt);
    }

    // 실패
    // 토큰이 유효하지 않은 경우
    [Fact]
    public async Task RevokeRefreshTokenAsync_Throws_WhenTokenInvalid()
    {
        // Arrange
        await using var db = AuthTestDb.CreateContext();

        // Act
        var service = new LogoutService(db, new StubJwtTokenService("hash"), NullLogger<LogoutService>.Instance);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RevokeRefreshTokenAsync(Guid.NewGuid(), "raw")); // InvalidOperationException 발생
    }

    // 실패
    // 이미 취소된 토큰
    [Fact]
    public async Task RevokeRefreshTokenAsync_Throws_WhenAlreadyRevoked()
    {
        // Arrange
        await using var db = AuthTestDb.CreateContext();
        var userId = Guid.NewGuid();
        var tokenHash = "token_hash";
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Act
        var service = new LogoutService(db, new StubJwtTokenService(tokenHash), NullLogger<LogoutService>.Instance);
        
        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RevokeRefreshTokenAsync(userId, "raw")); // InvalidOperationException 발생
    }

}
