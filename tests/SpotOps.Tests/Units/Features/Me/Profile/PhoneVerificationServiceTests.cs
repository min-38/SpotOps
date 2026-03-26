using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SpotOps.Data;
using SpotOps.Features.Me.Profile;
using SpotOps.Infrastructure.Sms;
using SpotOps.Models;

namespace SpotOps.Tests.Units.Features.Me.Profile;

public sealed class PhoneVerificationServiceTests
{
    private sealed class CaptureSmsSender : ISmsSender
    {
        public string? LastPhone { get; private set; }
        public string? LastMessage { get; private set; }

        public Task SendAsync(string toPhoneNumber, string message, CancellationToken cancellationToken = default)
        {
            LastPhone = toPhoneNumber;
            LastMessage = message;
            return Task.CompletedTask;
        }
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("phone_verify_" + Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task SendAndVerifyOtp_SetsPhoneVerifiedAt()
    {
        await using var db = CreateDb();
        var user = new User { Email = "u@test.com", PasswordHash = "x", Name = "U" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sms = new CaptureSmsSender();
        var svc = new PhoneVerificationService(db, sms, NullLogger<PhoneVerificationService>.Instance);

        var (sendOk, _, _) = await svc.SendOtpAsync(user.Id, "010-1234-5678");
        Assert.True(sendOk);
        Assert.Equal("01012345678", sms.LastPhone);
        Assert.NotNull(sms.LastMessage);

        var code = Regex.Match(sms.LastMessage!, @"\d{6}").Value;
        Assert.False(string.IsNullOrWhiteSpace(code));

        var (verifyOk, verifyCode, _) = await svc.VerifyOtpAsync(user.Id, code);
        Assert.True(verifyOk);
        Assert.Null(verifyCode);

        var saved = await db.Users.SingleAsync(u => u.Id == user.Id);
        Assert.Equal("01012345678", saved.Phone);
        Assert.NotNull(saved.PhoneVerifiedAt);
    }

    [Fact]
    public async Task VerifyOtp_WithWrongCode_ReturnsMismatch()
    {
        await using var db = CreateDb();
        var user = new User { Email = "u@test.com", PasswordHash = "x", Name = "U" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sms = new CaptureSmsSender();
        var svc = new PhoneVerificationService(db, sms, NullLogger<PhoneVerificationService>.Instance);
        await svc.SendOtpAsync(user.Id, "01011112222");

        var (ok, code, _) = await svc.VerifyOtpAsync(user.Id, "000000");
        Assert.False(ok);
        Assert.Equal("PHONE_OTP_MISMATCH", code);
    }
}

