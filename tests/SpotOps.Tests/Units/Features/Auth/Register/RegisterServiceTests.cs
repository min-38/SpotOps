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
            "new@test.com", "password123", "홍길동", null, UserRole.Buyer, null, null));

        Assert.True(success);
        Assert.Null(code);
        Assert.Null(error);
        Assert.True(db.Users.Any(u => u.Email == "new@test.com"));
    }

    // 실패: 이메일 중복
    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsError()
    {
        var db = CreateDb();
        var service = new RegisterService(db);

        await service.RegisterAsync(new RegisterDto(
            "dup@test.com", "password123", "홍길동", null, UserRole.Buyer, null, null));

        var (success, code, error) = await service.RegisterAsync(new RegisterDto(
            "dup@test.com", "password456", "김철수", null, UserRole.Buyer, null, null));

        Assert.False(success);
        Assert.Equal("AUTH_EMAIL_ALREADY_EXISTS", code);
        Assert.NotNull(error);
    }

    // 실패: 주최자 가입 시 필수 정보 누락
    [Fact]
    public async Task Register_AsOrganizer_CreatesOrganizerRecord()
    {
        var db = CreateDb();
        var service = new RegisterService(db);

        await service.RegisterAsync(new RegisterDto(
            "org@test.com", "password123", "주최자", null,
            UserRole.Organizer, "1234567890", "테스트기획사"));

        var user = db.Users.First(u => u.Email == "org@test.com");
        Assert.True(db.Organizers.Any(o => o.UserId == user.Id));
    }

}
