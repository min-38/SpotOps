using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SpotOps.Data;
using SpotOps.Features.Auth.PasswordReset;
using SpotOps.Infrastructure.Email;
using SpotOps.Models;

namespace SpotOps.Tests.Units.Features.Auth.PasswordReset;

public sealed class PasswordResetServiceTests
{
    private sealed class CaptureEmailSender : IEmailSender
    {
        public string? LastTo { get; private set; }
        public string? LastSubject { get; private set; }
        public string? LastBody { get; private set; }

        public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
        {
            LastTo = toEmail;
            LastSubject = subject;
            LastBody = body;
            return Task.CompletedTask;
        }
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("pw_reset_" + Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task RequestAsync_ForExistingUser_CreatesToken_AndSendsEmail()
    {
        await using var db = CreateDb();
        var user = new User { Email = "u@test.com", PasswordHash = "x", Name = "U" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sender = new CaptureEmailSender();
        var svc = new PasswordResetService(db, sender, NullLogger<PasswordResetService>.Instance);

        await svc.RequestAsync("u@test.com");

        var token = await db.PasswordResetTokens.FirstOrDefaultAsync(t => t.UserId == user.Id);
        Assert.NotNull(token);
        Assert.Equal("u@test.com", sender.LastTo);
        Assert.NotNull(sender.LastBody);
    }

    [Fact]
    public async Task ResetAsync_WithInvalidToken_ReturnsError()
    {
        await using var db = CreateDb();
        var sender = new CaptureEmailSender();
        var svc = new PasswordResetService(db, sender, NullLogger<PasswordResetService>.Instance);

        var (ok, code, _) = await svc.ResetAsync("bad-token", "newpassword123");

        Assert.False(ok);
        Assert.Equal("PASSWORD_RESET_TOKEN_INVALID", code);
    }

    [Fact]
    public async Task ResetAsync_WithWeakPassword_ReturnsWeakPasswordError()
    {
        await using var db = CreateDb();
        var sender = new CaptureEmailSender();
        var svc = new PasswordResetService(db, sender, NullLogger<PasswordResetService>.Instance);

        var (ok, code, _) = await svc.ResetAsync("0123456789ABCDEF", "password123");

        Assert.False(ok);
        Assert.Equal("PASSWORD_RESET_PASSWORD_WEAK", code);
    }

    [Fact]
    public async Task RequestAsync_NonExistingEmail_TooManyRequests_ReturnsRateLimited()
    {
        await using var db = CreateDb();
        var sender = new CaptureEmailSender();
        var svc = new PasswordResetService(db, sender, NullLogger<PasswordResetService>.Instance);

        for (var i = 0; i < 5; i++)
        {
            var (ok, code, _) = await svc.RequestAsync("nobody@test.com");
            Assert.True(ok);
            Assert.Null(code);
        }

        var (ok6, code6, _) = await svc.RequestAsync("nobody@test.com");
        Assert.False(ok6);
        Assert.Equal("PASSWORD_RESET_RATE_LIMITED", code6);
    }
}

