using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SpotOps.Data;
using SpotOps.Features.Auth.Login;
using SpotOps.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SpotOps.Tests.Units.Features.Auth.Login;

public class LoginServiceTests
{
    // InMemory DB 사용
    // 테스트마다 새로운 DB 생성하여 독립성 보장
    // 테스트끼리 데이터 충돌 방지
    // 즉, 테스트마다 깨끗한 DB 환경에서 실행됨
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private User CreateUser(AppDbContext db, string email = "test@test.com", string password = "password123")
    {
        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Name = "테스트유저",
            Role = UserRole.Buyer
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    private static IConfiguration CreateJwtConfig()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT_ISSUER"] = "spotops-test",
                ["JWT_AUDIENCE"] = "spotops-test-client",
                ["JWT_SECRET"] = "spotops_test_secret_for_unit_tests_12345",
                ["JWT_ACCESS_TOKEN_EXPIRES_SECONDS"] = "3600"
            })
            .Build();
    }

    // 로그인 성공
    [Fact] // <- xUnit에서 테스트 메서드임을 나타내는 어트리뷰트
    public async Task Login_WithValidCredentials_ReturnsUser()
    {
        // Arrange
        var db = CreateDb();
        CreateUser(db);
        var service = new LoginService(db, CreateJwtConfig());

        // Act
        var (user, error) = await service.ValidateAsync(new LoginDto("test@test.com", "password123"));

        // Assert
        Assert.NotNull(user);
        Assert.Null(error);
    }

    // 잘못된 비밀번호
    [Fact]
    public async Task Login_WithWrongPassword_ReturnsError()
    {
        // Arrange
        var db = CreateDb();
        CreateUser(db);
        var service = new LoginService(db, CreateJwtConfig());

        // Act
        var (user, error) = await service.ValidateAsync(new LoginDto("test@test.com", "wrongpassword"));

        // Assert
        Assert.Null(user);
        Assert.NotNull(error);
    }

    // 없는 이메일
    [Fact]
    public async Task Login_WithNonExistentEmail_ReturnsError()
    {
        // Arrange
        var db = CreateDb();
        var service = new LoginService(db, CreateJwtConfig());

        // Act
        var (user, error) = await service.ValidateAsync(new LoginDto("nobody@test.com", "password123"));

        // Assert
        Assert.Null(user);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task CreateTokenPairAsync_ReturnsAccessAndRefreshTokens()
    {
        await using var db = CreateDb();
        var user = CreateUser(db);
        var service = new LoginService(db, CreateJwtConfig());

        var tokens = await service.CreateTokenPairAsync(user);

        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
        Assert.Equal("Bearer", tokens.TokenType);
        Assert.True(tokens.ExpiresInSeconds > 0);
        Assert.True(tokens.RefreshTokenExpiresInSeconds > 0);
        Assert.True(await db.RefreshTokens.AnyAsync(t => t.UserId == user.Id));
    }

    [Fact]
    public async Task CreateAccessToken_ContainsUserClaims()
    {
        await using var db = CreateDb();
        var user = CreateUser(db, "claims@test.com", "password123");
        var service = new LoginService(db, CreateJwtConfig());

        var (token, _) = service.CreateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(user.Id.ToString(), jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal(user.Email, jwt.Claims.First(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal(user.Role.ToString(), jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public async Task RefreshAsync_WithValidToken_RotatesRefreshToken()
    {
        await using var db = CreateDb();
        var user = CreateUser(db, "rotate@test.com", "password123");
        var service = new LoginService(db, CreateJwtConfig());
        var first = await service.CreateTokenPairAsync(user);

        var (refreshedUser, refreshedTokens, code, _) = await service.RefreshAsync(first.RefreshToken);

        Assert.Null(code);
        Assert.NotNull(refreshedUser);
        Assert.NotNull(refreshedTokens);
        Assert.NotEqual(first.RefreshToken, refreshedTokens!.RefreshToken);
        Assert.Equal(2, await db.RefreshTokens.CountAsync(t => t.UserId == user.Id));
        Assert.Equal(1, await db.RefreshTokens.CountAsync(t => t.UserId == user.Id && t.RevokedAt != null));
    }

    [Fact]
    public async Task RefreshAsync_WithRevokedToken_ReturnsInvalid()
    {
        await using var db = CreateDb();
        var user = CreateUser(db, "revoked@test.com", "password123");
        var service = new LoginService(db, CreateJwtConfig());
        var first = await service.CreateTokenPairAsync(user);
        await service.RefreshAsync(first.RefreshToken);

        var (refreshedUser, refreshedTokens, code, _) = await service.RefreshAsync(first.RefreshToken);

        Assert.Null(refreshedUser);
        Assert.Null(refreshedTokens);
        Assert.Equal("AUTH_REFRESH_TOKEN_INVALID", code);
    }
}
