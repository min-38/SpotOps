using Microsoft.EntityFrameworkCore;
using SpotOps.Data;
using SpotOps.Models;

namespace SpotOps.Tests.Auth;

internal static class AuthTestDb
{
    public static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    public static User CreateUser(
        string? email = null,
        string name = "Tester",
        string phone = "01012345678",
        string rawPassword = "Correct123!")
    {
        var now = DateTime.UtcNow;
        return new User
        {
            Email = email ?? $"user-{Guid.NewGuid():N}@example.com",
            Name = name,
            Phone = phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword),
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
