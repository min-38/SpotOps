using Microsoft.EntityFrameworkCore;
using SpotOps.Data;
using SpotOps.Features.Auth.Register;
using SpotOps.Models;

namespace SpotOps.Tests.Units.Features.Auth.Register;

public class RegisterServiceTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    // 회원가입 성공
    [Fact]
    public async Task Register_WithValidData_Succeeds()
    {
        var db = CreateDb();
        var service = new RegisterService(db);

        var (success, code, error) = await service.RegisterAsync(new RegisterDto(
            "new@test.com", "password123", "홍길동", null));

        Assert.True(success);
        Assert.Null(code);
        Assert.Null(error);
        Assert.True(db.Users.Any(u => u.Email == "new@test.com"));
        Assert.Equal(UserRole.Buyer, db.Users.First(u => u.Email == "new@test.com").Role);
    }

    // 실패: 이메일 중복
    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsError()
    {
        var db = CreateDb();
        var service = new RegisterService(db);

        await service.RegisterAsync(new RegisterDto(
            "dup@test.com", "password123", "홍길동", null));

        var (success, code, error) = await service.RegisterAsync(new RegisterDto(
            "dup@test.com", "password456", "김철수", null));

        Assert.False(success);
        Assert.Equal("AUTH_EMAIL_ALREADY_EXISTS", code);
        Assert.NotNull(error);
    }

    // 회원가입은 항상 구매자 역할로 생성
    [Fact]
    public async Task Register_AlwaysCreatesBuyer()
    {
        var db = CreateDb();
        var service = new RegisterService(db);

        await service.RegisterAsync(new RegisterDto(
            "buyer@test.com", "password123", "일반회원", null));

        var user = db.Users.First(u => u.Email == "buyer@test.com");
        Assert.Equal(UserRole.Buyer, user.Role);
        Assert.False(db.Organizers.Any(o => o.UserId == user.Id));
    }

}
