using Microsoft.EntityFrameworkCore;
using SpotOps.Data;
using SpotOps.Features.Me.Profile;
using SpotOps.Models;

namespace SpotOps.Tests.Units.Features.Me.Profile;

public sealed class MyProfileServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("my_profile_" + Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetAsync_ReturnsProfile()
    {
        await using var db = CreateDb();
        var user = new User
        {
            Email = "u@test.com",
            PasswordHash = "x",
            Name = "Old Name",
            Phone = "010-1111-2222",
            Role = UserRole.Buyer
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var svc = new MyProfileService(db);
        var profile = await svc.GetAsync(user.Id);

        Assert.NotNull(profile);
        Assert.Equal(user.Id, profile!.Id);
        Assert.Equal("u@test.com", profile.Email);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesNameAndPhone()
    {
        await using var db = CreateDb();
        var user = new User
        {
            Email = "u@test.com",
            PasswordHash = "x",
            Name = "Old Name",
            Phone = "010-1111-2222",
            Role = UserRole.Buyer
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var svc = new MyProfileService(db);
        var (profile, code, error) = await svc.UpdateAsync(
            user.Id,
            new UpdateMyProfileRequest("New Name", "010-9999-8888"));

        Assert.Null(code);
        Assert.Null(error);
        Assert.NotNull(profile);
        Assert.Equal("New Name", profile!.Name);
        Assert.Equal("010-9999-8888", profile.Phone);
    }

    [Fact]
    public async Task UpdateAsync_WithBlankName_ReturnsValidationError()
    {
        await using var db = CreateDb();
        var user = new User { Email = "u@test.com", PasswordHash = "x", Name = "Old Name" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var svc = new MyProfileService(db);
        var (profile, code, _) = await svc.UpdateAsync(user.Id, new UpdateMyProfileRequest("   ", null));

        Assert.Null(profile);
        Assert.Equal("ME_PROFILE_NAME_REQUIRED", code);
    }
}

