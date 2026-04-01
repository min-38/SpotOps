using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SpotOps.Features.Auth;
using SpotOps.Features.Auth.Register;
using SpotOps.Infrastructure.PortOne;
using SpotOps.Infrastructure.Redis;
using StackExchange.Redis;

namespace SpotOps.Tests.Auth;

public class RegisterServiceTests
{
    // 실패
    // 본인인증 검증 실패의 경우
    [Fact]
    public async Task VerifyIvAsync_ReturnsFailed_WhenPortOneVerificationFails()
    {
        await using var db = AuthTestDb.CreateContext();
        var portOne = new Mock<IPortOneIvVerifyService>();
        var redis = new Mock<IDatabase>();
        portOne.Setup(x => x.VerifyAsync("iv_fail", It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, (JsonElement?)null, "AUTH_VERIFY_IV_FAILED"));

        var service = CreateService(db, portOne.Object, redis.Object);
        var (success, verifiedIdentity, errorCode) = await service.VerifyIvAsync(new PortOneIvVerifyRequest("iv_fail"));

        Assert.False(success);
        Assert.Null(verifiedIdentity);
        Assert.Equal("AUTH_VERIFY_IV_FAILED", errorCode);
    }

    // 성공
    // 본인인증 검증 성공의 경우
    [Fact]
    public async Task VerifyIvAsync_ReturnsSuccess_WhenPortOneVerificationSucceeds()
    {
        await using var db = AuthTestDb.CreateContext();
        var portOne = new Mock<IPortOneIvVerifyService>();
        var redis = new Mock<IDatabase>();

        var identity = CreateIdentityJson("""
        {
          "name":"Tester",
          "gender":"male",
          "birthday":"2000-01-01",
          "unique_key":"ci_123",
          "phone":"010-1234-5678",
          "di":"di_123"
        }
        """);

        portOne.Setup(x => x.VerifyAsync("iv_ok", It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, (JsonElement?)identity, (string?)null));
        redis.Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var service = CreateService(db, portOne.Object, redis.Object);
        var (success, verifiedIdentity, errorCode) = await service.VerifyIvAsync(new PortOneIvVerifyRequest("iv_ok"));

        Assert.True(success);
        Assert.NotNull(verifiedIdentity);
        Assert.Null(errorCode);
        Assert.False(string.IsNullOrWhiteSpace(verifiedIdentity!.VerificationToken));
        Assert.Equal("Tester", verifiedIdentity.Name);
        Assert.Equal("MALE", verifiedIdentity.Gender);
        Assert.Equal("2000-01-01", verifiedIdentity.Birthday);
        Assert.Equal("01012345678", verifiedIdentity.PhoneNumber);
    }

    // 실패
    // 검증 토큰이 없는 경우
    [Fact]
    public async Task RegisterAsync_ReturnsInvalidRequest_WhenVerificationTokenNotFoundInRedis()
    {
        await using var db = AuthTestDb.CreateContext();
        var redis = new Mock<IDatabase>();
        redis.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var service = CreateService(db, new Mock<IPortOneIvVerifyService>().Object, redis.Object);
        var request = CreateRegisterRequest(verificationToken: "missing");
        var (success, errorCode) = await service.RegisterAsync(request);

        Assert.False(success);
        Assert.Equal("AUTH_REGISTER_INVALID_REQUEST", errorCode);
    }

    // 실패
    // 본인인증 검증 결과 불일치의 경우
    [Fact]
    public async Task RegisterAsync_ReturnsRetryExceeded_WhenIdentityMismatchExceedsLimit()
    {
        await using var db = AuthTestDb.CreateContext();
        var redis = new Mock<IDatabase>();
        var cachedJson = CreateCachedJson(failedAttempts: 4, name: "Tester");

        redis.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)cachedJson);
        redis.Setup(x => x.KeyTimeToLiveAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(TimeSpan.FromMinutes(10));
        redis.Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var service = CreateService(db, new Mock<IPortOneIvVerifyService>().Object, redis.Object);
        var request = CreateRegisterRequest(name: "Tampered");
        var (success, errorCode) = await service.RegisterAsync(request);

        Assert.False(success);
        Assert.Equal("AUTH_VERIFY_IV_RETRY_EXCEEDED", errorCode);
    }

    // 실패
    // 이메일이 이미 존재하는 경우
    [Fact]
    public async Task RegisterAsync_ReturnsEmailAlreadyExists_WhenDuplicateEmail()
    {
        await using var db = AuthTestDb.CreateContext();
        var existing = AuthTestDb.CreateUser(email: "dup@example.com");
        db.Users.Add(existing);
        await db.SaveChangesAsync();

        var redis = new Mock<IDatabase>();
        redis.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)CreateCachedJson(name: "Tester"));

        var service = CreateService(db, new Mock<IPortOneIvVerifyService>().Object, redis.Object);
        var request = CreateRegisterRequest(email: "dup@example.com");
        var (success, errorCode) = await service.RegisterAsync(request);

        Assert.False(success);
        Assert.Equal("AUTH_EMAIL_ALREADY_EXISTS", errorCode);
    }

    // 성공
    // 정상 요청의 경우
    [Fact]
    public async Task RegisterAsync_CreatesUserAndVerification_WhenRequestValid()
    {
        await using var db = AuthTestDb.CreateContext();
        var redis = new Mock<IDatabase>();
        redis.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)CreateCachedJson(name: "Tester"));
        redis.Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var service = CreateService(db, new Mock<IPortOneIvVerifyService>().Object, redis.Object);
        var request = CreateRegisterRequest(email: "new@example.com", name: "Tester", phone: "010-1234-5678");
        var (success, errorCode) = await service.RegisterAsync(request);

        Assert.True(success);
        Assert.Null(errorCode);
        Assert.Single(db.Users);
        Assert.Single(db.UserVerifications);
        redis.Verify(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    private static RegisterService CreateService(
        SpotOps.Data.AppDbContext db,
        IPortOneIvVerifyService portOne,
        IDatabase redis)
    {
        var redisOptions = new RedisOptions { KeyPrefix = "spotops-test" };
        var protector = new PassThroughSensitiveDataProtector();
        return new RegisterService(
            db,
            portOne,
            redis,
            redisOptions,
            protector,
            NullLogger<RegisterService>.Instance);
    }

    private static RegisterRequest CreateRegisterRequest(
        string verificationToken = "token123",
        string name = "Tester",
        string gender = "MALE",
        string birthday = "2000-01-01",
        string uniqueKey = "CI_123",
        string phone = "01012345678",
        string email = "user@example.com",
        string password = "Abcd1234!")
    {
        return new RegisterRequest(
            VerificationToken: verificationToken,
            Name: name,
            Gender: gender,
            Birthday: birthday,
            UniqueKey: uniqueKey,
            Phone: phone,
            Email: email,
            Password: password,
            PasswordConfirmation: password);
    }

    private static JsonElement CreateIdentityJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static string CreateCachedJson(
        int failedAttempts = 0,
        string name = "Tester",
        string gender = "MALE",
        string birthday = "2000-01-01",
        string uniqueKey = "CI_123",
        string phone = "01012345678")
    {
        return JsonSerializer.Serialize(new
        {
            IdentityVerificationId = "iv_123",
            Name = name,
            Gender = gender,
            Birthday = birthday,
            UniqueKey = uniqueKey,
            PhoneNumber = phone,
            Di = "DI_123",
            VerifiedAtUtc = DateTime.UtcNow,
            FailedAttempts = failedAttempts
        });
    }

    private sealed class PassThroughSensitiveDataProtector : ISensitiveDataProtector
    {
        public string? Protect(string? value) => value;
        public string? Unprotect(string? protectedValue) => protectedValue;
    }
}
