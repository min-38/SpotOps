using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SpotOps.Features.Auth.ForgotPassword;
using SpotOps.Features.Auth.ResetPassword;
using SpotOps.Infrastructure.Email;
using SpotOps.Models;

namespace SpotOps.Tests.Auth;

public class ResetPasswordServiceTests
{
    // 성공
    // 유저가 존재하지 않는 경우
    [Fact]
    public async Task ForgotPasswordAsync_ReturnsSuccess_WhenUserDoesNotExist()
    {
        await using var db = AuthTestDb.CreateContext();
        var sender = new FakeEmailSender();
        var service = new ForgotPasswordService(db, sender, CreateConfiguration(), NullLogger<ForgotPasswordService>.Instance);

        var (success, code) = await service.ForgotPasswordAsync(new ForgotPasswordRequest("nobody@example.com"));

        Assert.True(success);
        Assert.Null(code);
        Assert.Empty(db.PasswordResetTokens);
        Assert.Null(sender.LastToEmail);
    }

    // 성공
    // 유저가 존재하는 경우
    [Fact]
    public async Task ForgotPasswordAsync_SavesTokenAndSendsLink_WhenUserExists()
    {
        await using var db = AuthTestDb.CreateContext();
        var user = AuthTestDb.CreateUser(email: "user@example.com", name: "User", phone: "01012345678", rawPassword: "Abcd1234!");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sender = new FakeEmailSender();
        var service = new ForgotPasswordService(db, sender, CreateConfiguration(), NullLogger<ForgotPasswordService>.Instance);

        var (success, code) = await service.ForgotPasswordAsync(new ForgotPasswordRequest(user.Email));

        Assert.True(success);
        Assert.Null(code);
        Assert.Single(db.PasswordResetTokens);
        Assert.Equal(user.Email, sender.LastToEmail);
        Assert.Contains("/auth/password-reset?token=", sender.LastBody);
    }

    // 실패
    // 토큰이 만료된 경우
    [Fact]
    public async Task ValidateResetTokenAsync_ReturnsFalse_WhenExpired()
    {
        await using var db = AuthTestDb.CreateContext();
        var token = "0123456789ABCDEF0123456789ABCDEF";
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = Guid.NewGuid(),
            TokenHash = Hash(token),
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            CreatedAt = DateTime.UtcNow.AddMinutes(-31)
        });
        await db.SaveChangesAsync();

        var service = new ResetPasswordService(db);
        var (ok, code) = await service.ValidateResetTokenAsync(token);

        Assert.False(ok);
        Assert.Equal("PASSWORD_RESET_TOKEN_EXPIRED", code);
    }

    // 실패
    // 토큰이 취소된 경우
    [Fact]
    public async Task ValidateResetTokenAsync_ReturnsFalse_WhenRevoked()
    {
        await using var db = AuthTestDb.CreateContext();
        var token = "11112222333344445555666677778888";
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = Guid.NewGuid(),
            TokenHash = Hash(token),
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            RevokedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new ResetPasswordService(db);
        var (ok, code) = await service.ValidateResetTokenAsync(token);

        Assert.False(ok);
        Assert.Equal("PASSWORD_RESET_TOKEN_EXPIRED", code);
    }

    // 성공
    // 정상 요청의 경우
    [Fact]
    public async Task ResetPasswordAsync_ChangesPasswordAndMarksTokenUsed_WhenValid()
    {
        await using var db = AuthTestDb.CreateContext();
        var token = "ABCDEF0123456789ABCDEF0123456789";
        var user = AuthTestDb.CreateUser(email: "reset@example.com", name: "Reset User", phone: "01011112222", rawPassword: "Abcd1234!");
        db.Users.Add(user);
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = Hash(token),
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new ResetPasswordService(db);
        var request = new ResetPasswordRequest(token, "Xyz!12345", "Xyz!12345");
        var (ok, code) = await service.ResetPasswordAsync(request);

        Assert.True(ok);
        Assert.Null(code);

        var savedToken = await db.PasswordResetTokens.FirstAsync();
        Assert.NotNull(savedToken.UsedAt);

        var savedUser = await db.Users.FirstAsync();
        Assert.True(BCrypt.Net.BCrypt.Verify("Xyz!12345", savedUser.PasswordHash));
    }

    // 실패
    // 토큰이 이미 사용된 경우
    [Fact]
    public async Task ResetPasswordAsync_ReturnsExpired_WhenTokenAlreadyUsed()
    {
        await using var db = AuthTestDb.CreateContext();
        var token = "ABCDEF0123456789ABCDEF0000000000";
        var user = AuthTestDb.CreateUser(email: "used@example.com", name: "Used User", phone: "01011112222", rawPassword: "Abcd1234!");
        db.Users.Add(user);
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = Hash(token),
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            UsedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new ResetPasswordService(db);
        var request = new ResetPasswordRequest(token, "Xyz!12345", "Xyz!12345");
        var (ok, code) = await service.ResetPasswordAsync(request);

        Assert.False(ok);
        Assert.Equal("PASSWORD_RESET_TOKEN_EXPIRED", code);
    }

    private static IConfiguration CreateConfiguration()
    {
        var values = new Dictionary<string, string?>
        {
            ["APP_URL"] = "https://app.example.com"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static string Hash(string value)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public string? LastToEmail { get; private set; }
        public string? LastBody { get; private set; }

        public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
        {
            LastToEmail = toEmail;
            LastBody = body;
            return Task.CompletedTask;
        }
    }
}
