using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SpotOps.Features.Auth.Login;
using SpotOps.Models;
using SpotOps.Data;

namespace SpotOps.Tests.Auth;

public class LoginServiceTests
{
    // 실패
    // 비밀번호 틀린 경우
    [Fact]
    public async Task ValidateAsync_ReturnsNull_WhenPasswordIsWrong()
    {
        await using var db = CreateDbContext();
        db.Users.Add(AuthTestDb.CreateUser(email: "user@example.com", phone: "01012345678"));
        await db.SaveChangesAsync();

        var service = new LoginService(db, new StubJwtTokenService(), NullLogger<LoginService>.Instance);
        var user = await service.ValidateAsync("user@example.com", "Wrong123!");

        Assert.Null(user);
    }

    // 성공
    // 로그인 시 refresh_tokens에 해시 저장
    [Fact]
    public async Task CreateTokenPairAsync_PersistsRefreshToken_WhenCalled()
    {
        await using var db = CreateDbContext();
        var user = AuthTestDb.CreateUser(phone: "01012345678");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var jwt = new StubJwtTokenService
        {
            NewRefreshToken = "login_refresh_raw",
            NewRefreshHash = "login_refresh_hash"
        };
        var service = new LoginService(db, jwt, NullLogger<LoginService>.Instance);

        var tokens = await service.CreateTokenPairAsync(user);

        Assert.Equal("login_refresh_raw", tokens.RefreshToken);
        Assert.Equal(14L * 24 * 60 * 60, tokens.RefreshTokenExpiresInSeconds);

        var stored = await db.RefreshTokens.SingleAsync();
        Assert.Equal(user.Id, stored.UserId);
        Assert.Equal("login_refresh_hash", stored.TokenHash);
        Assert.Null(stored.RevokedAt);
        Assert.True(stored.ExpiresAt > DateTime.UtcNow);
    }

    // 성공
    // 정상 요청의 경우
    [Fact]
    public async Task ValidateAsync_ReturnsUser_WhenCredentialsValid()
    {
        await using var db = CreateDbContext();
        var targetUser = AuthTestDb.CreateUser(phone: "01012345678");
        db.Users.Add(targetUser);
        await db.SaveChangesAsync();

        var service = new LoginService(db, new StubJwtTokenService(), NullLogger<LoginService>.Instance);
        var user = await service.ValidateAsync(targetUser.Email, "Correct123!");

        Assert.NotNull(user);
        Assert.Equal(targetUser.Email, user!.Email);
    }

    // 실패
    // 토큰이 없는 경우
    [Fact]
    public async Task RefreshTokenAsync_ReturnsInvalid_WhenTokenNotFound()
    {
        await using var db = CreateDbContext();
        var jwt = new StubJwtTokenService { HashResult = "missing_hash" };
        var service = new LoginService(db, jwt, NullLogger<LoginService>.Instance);

        var (user, tokens, errorCode) = await service.RefreshTokenAsync("refresh_token");

        Assert.Null(user);
        Assert.Null(tokens);
        Assert.Equal("AUTH_REFRESH_TOKEN_INVALID", errorCode);
    }

    // 실패
    // 토큰이 만료된 경우
    [Fact]
    public async Task RefreshTokenAsync_ReturnsInvalid_WhenTokenExpired()
    {
        await using var db = CreateDbContext();
        var user = AuthTestDb.CreateUser(phone: "010");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var jwt = new StubJwtTokenService { HashResult = "expired_hash" };
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = "expired_hash",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        });
        await db.SaveChangesAsync();

        var service = new LoginService(db, jwt, NullLogger<LoginService>.Instance);
        var (outUser, tokens, errorCode) = await service.RefreshTokenAsync("refresh_token");

        Assert.Null(outUser);
        Assert.Null(tokens);
        Assert.Equal("AUTH_REFRESH_TOKEN_INVALID", errorCode);
    }

    // 실패
    // 토큰이 취소된 경우
    [Fact]
    public async Task RefreshTokenAsync_ReturnsInvalid_WhenTokenRevoked()
    {
        await using var db = CreateDbContext();
        var user = AuthTestDb.CreateUser(phone: "010");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var jwt = new StubJwtTokenService { HashResult = "revoked_hash" };
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = "revoked_hash",
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            RevokedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new LoginService(db, jwt, NullLogger<LoginService>.Instance);
        var (outUser, tokens, errorCode) = await service.RefreshTokenAsync("refresh_token");

        Assert.Null(outUser);
        Assert.Null(tokens);
        Assert.Equal("AUTH_REFRESH_TOKEN_INVALID", errorCode);
    }

    // 성공
    // 토큰이 유효한 경우
    [Fact]
    public async Task RefreshTokenAsync_RotatesTokens_WhenTokenValid()
    {
        await using var db = CreateDbContext();
        var user = AuthTestDb.CreateUser(phone: "010");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var jwt = new StubJwtTokenService
        {
            HashResult = "old_hash",
            NewRefreshToken = "new_refresh",
            NewRefreshHash = "new_hash"
        };
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = "old_hash",
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        });
        await db.SaveChangesAsync();

        var service = new LoginService(db, jwt, NullLogger<LoginService>.Instance);
        var (outUser, tokens, errorCode) = await service.RefreshTokenAsync("old_raw_token");

        Assert.NotNull(outUser);
        Assert.NotNull(tokens);
        Assert.Null(errorCode);
        Assert.Equal("new_refresh", tokens!.RefreshToken);

        var oldToken = await db.RefreshTokens.SingleAsync(x => x.TokenHash == "old_hash");
        var newToken = await db.RefreshTokens.SingleAsync(x => x.TokenHash == "new_hash");
        Assert.NotNull(oldToken.RevokedAt);
        Assert.Null(newToken.RevokedAt);
        Assert.Equal(user.Id, newToken.UserId);
    }

    /// <summary>
    /// In-memory 데이터베이스 컨텍스트 생성
    /// </summary>
    /// <returns>AppDbContext</returns>
    private static AppDbContext CreateDbContext() => AuthTestDb.CreateContext();

}
