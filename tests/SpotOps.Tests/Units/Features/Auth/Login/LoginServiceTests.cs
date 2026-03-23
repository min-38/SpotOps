using Microsoft.EntityFrameworkCore;
using SpotOps.Data;
using SpotOps.Features.Auth.Login;
using SpotOps.Models;

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

    // 로그인 성공
    [Fact] // <- xUnit에서 테스트 메서드임을 나타내는 어트리뷰트
    public async Task Login_WithValidCredentials_ReturnsUser()
    {
        // Arrange
        var db = CreateDb();
        CreateUser(db);
        var service = new LoginService(db);

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
        var service = new LoginService(db);

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
        var service = new LoginService(db);

        // Act
        var (user, error) = await service.ValidateAsync(new LoginDto("nobody@test.com", "password123"));

        // Assert
        Assert.Null(user);
        Assert.NotNull(error);
    }
}
