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
        var (success, verifiedIdentity, errorCode, existingMaskedEmail) = await service.VerifyIvAsync(new PortOneIvVerifyRequest("iv_fail"));

        Assert.False(success);
        Assert.Null(verifiedIdentity);
        Assert.Equal("AUTH_VERIFY_IV_FAILED", errorCode);
        Assert.Null(existingMaskedEmail);
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
        var (success, verifiedIdentity, errorCode, existingMaskedEmail) = await service.VerifyIvAsync(new PortOneIvVerifyRequest("iv_ok"));

        Assert.True(success);
        Assert.NotNull(verifiedIdentity);
        Assert.Null(errorCode);
        Assert.Null(existingMaskedEmail);
        Assert.False(string.IsNullOrWhiteSpace(verifiedIdentity!.VerificationToken));
        Assert.Equal("Tester", verifiedIdentity.Name);
        Assert.Equal("MALE", verifiedIdentity.Gender);
        Assert.Equal("2000-01-01", verifiedIdentity.Birthday);
        Assert.Equal("01012345678", verifiedIdentity.PhoneNumber);
    }

    [Fact]
    public async Task VerifyIvAsync_ReturnsSuccess_WhenPortOneResponseUsesVerifiedCustomerShape()
    {
        await using var db = AuthTestDb.CreateContext();
        var portOne = new Mock<IPortOneIvVerifyService>();
        var redis = new Mock<IDatabase>();

        var identity = CreateIdentityJson("""
        {
          "status":"VERIFIED",
          "id":"iv_hidden",
          "verifiedCustomer":{
            "id":"ci_from_verified_customer",
            "name":"Annonymous",
            "phoneNumber":"01012345678",
            "birthDate":"2000-01-01",
            "gender":"FEMALE",
            "isForeigner":false
          }
        }
        """);

        portOne.Setup(x => x.VerifyAsync("iv_nested", It.IsAny<CancellationToken>()))
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
        var (success, verifiedIdentity, errorCode, existingMaskedEmail) = await service.VerifyIvAsync(new PortOneIvVerifyRequest("iv_nested"));

        Assert.True(success);
        Assert.NotNull(verifiedIdentity);
        Assert.Null(errorCode);
        Assert.Null(existingMaskedEmail);
        Assert.Equal("Annonymous", verifiedIdentity!.Name);
        Assert.Equal("FEMALE", verifiedIdentity.Gender);
        Assert.Equal("2000-01-01", verifiedIdentity.Birthday);
        Assert.Equal("CI_FROM_VERIFIED_CUSTOMER", verifiedIdentity.UniqueKey);
        Assert.Equal("01012345678", verifiedIdentity.PhoneNumber);
    }

    [Fact]
    public async Task VerifyIvAsync_ReturnsExistingAccountCode_WithMaskedEmail_WhenAccountAlreadyExists()
    {
        await using var db = AuthTestDb.CreateContext();
        var existing = AuthTestDb.CreateUser(email: "example@gmail.com");
        db.Users.Add(existing);
        db.UserVerifications.Add(new SpotOps.Models.UserVerification
        {
            UserId = existing.Id,
            Status = SpotOps.Models.VerificationStatus.Verified,
            Ci = "CI_123"
        });
        await db.SaveChangesAsync();

        var portOne = new Mock<IPortOneIvVerifyService>();
        var redis = new Mock<IDatabase>();
        var identity = CreateIdentityJson("""
        {
          "name":"Tester",
          "gender":"MALE",
          "birthday":"2000-01-01",
          "unique_key":"ci_123",
          "phone":"01012345678"
        }
        """);
        portOne.Setup(x => x.VerifyAsync("iv_exists", It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, (JsonElement?)identity, (string?)null));

        var service = CreateService(db, portOne.Object, redis.Object);
        var (success, verifiedIdentity, errorCode, existingMaskedEmail) = await service.VerifyIvAsync(new PortOneIvVerifyRequest("iv_exists"));

        Assert.False(success);
        Assert.Null(verifiedIdentity);
        Assert.Equal("AUTH_REGISTER_ALREADY_EXISTS", errorCode);
        Assert.Equal("ex******le@gmail.com", existingMaskedEmail);
    }

    [Fact]
    public async Task VerifyIvAsync_ReturnsExistingAccountCode_WhenCiDiffersButProfileMatches()
    {
        await using var db = AuthTestDb.CreateContext();
        var existing = AuthTestDb.CreateUser(email: "sameperson@gmail.com", name: "Annonymous", phone: "010-1234-5678");
        db.Users.Add(existing);
        db.UserVerifications.Add(new SpotOps.Models.UserVerification
        {
            UserId = existing.Id,
            Status = SpotOps.Models.VerificationStatus.Verified,
            BirthDate = new DateOnly(2000, 1, 1),
            Ci = "OLD_PROVIDER_KEY"
        });
        await db.SaveChangesAsync();

        var portOne = new Mock<IPortOneIvVerifyService>();
        var redis = new Mock<IDatabase>();
        var identity = CreateIdentityJson("""
        {
          "name":"Annonymous",
          "gender":"FEMALE",
          "birthday":"2000-01-01",
          "unique_key":"NEW_PROVIDER_KEY",
          "phone":"01012345678"
        }
        """);
        portOne.Setup(x => x.VerifyAsync("iv_profile_match", It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, (JsonElement?)identity, (string?)null));

        var service = CreateService(db, portOne.Object, redis.Object);
        var (success, verifiedIdentity, errorCode, existingMaskedEmail) = await service.VerifyIvAsync(new PortOneIvVerifyRequest("iv_profile_match"));

        Assert.False(success);
        Assert.Null(verifiedIdentity);
        Assert.Equal("AUTH_REGISTER_ALREADY_EXISTS", errorCode);
        Assert.Equal("sa******on@gmail.com", existingMaskedEmail);
    }

    [Fact]
    public async Task VerifyIvAsync_MasksShortEmailSafely_WhenAccountAlreadyExists()
    {
        await using var db = AuthTestDb.CreateContext();
        var existing = AuthTestDb.CreateUser(email: "ab@gmail.com");
        db.Users.Add(existing);
        db.UserVerifications.Add(new SpotOps.Models.UserVerification
        {
            UserId = existing.Id,
            Status = SpotOps.Models.VerificationStatus.Verified,
            Ci = "CI_SHORT"
        });
        await db.SaveChangesAsync();

        var portOne = new Mock<IPortOneIvVerifyService>();
        var redis = new Mock<IDatabase>();
        var identity = CreateIdentityJson("""
        {
          "name":"Tester",
          "gender":"MALE",
          "birthday":"2000-01-01",
          "unique_key":"ci_short",
          "phone":"01012345678"
        }
        """);
        portOne.Setup(x => x.VerifyAsync("iv_short_email", It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, (JsonElement?)identity, (string?)null));

        var service = CreateService(db, portOne.Object, redis.Object);
        var (success, verifiedIdentity, errorCode, existingMaskedEmail) = await service.VerifyIvAsync(new PortOneIvVerifyRequest("iv_short_email"));

        Assert.False(success);
        Assert.Null(verifiedIdentity);
        Assert.Equal("AUTH_REGISTER_ALREADY_EXISTS", errorCode);
        Assert.Equal("a******@gmail.com", existingMaskedEmail);
    }

    // 실패
    // 검증 토큰이 Redis에 없거나 만료된 경우 (TTL 경과 시 키 삭제 → GET 결과가 비어 있음과 동일)
    [Fact]
    public async Task RegisterAsync_ReturnsIvExpired_WhenVerificationTokenMissingOrExpired()
    {
        await using var db = AuthTestDb.CreateContext();
        var portOne = new Mock<IPortOneIvVerifyService>().Object;

        foreach (var cached in new[] { RedisValue.Null, (RedisValue)"" })
        {
            var redis = new Mock<IDatabase>();
            redis.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(cached);

            var service = CreateService(db, portOne, redis.Object);
            var (success, errorCode) = await service.RegisterAsync(
                CreateRegisterRequest(verificationToken: "expired-or-missing"));

            Assert.False(success);
            Assert.Equal("AUTH_VERIFY_IV_EXPIRED", errorCode);
        }
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
