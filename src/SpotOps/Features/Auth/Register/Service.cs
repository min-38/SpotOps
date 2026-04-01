using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SpotOps.Data;
using SpotOps.Features.Auth;
using SpotOps.Infrastructure.PortOne;
using SpotOps.Infrastructure.Redis;
using SpotOps.Models;
using StackExchange.Redis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using VerificationStatus = SpotOps.Models.VerificationStatus;

namespace SpotOps.Features.Auth.Register;

public sealed partial class RegisterService : IRegisterService
{
    private const int VerificationTtlMinutes = 15;
    private const int MaxRegisterAttempts = 5;

    private readonly AppDbContext _db;
    private readonly IPortOneIvVerifyService _portOneIvService;
    private readonly IDatabase _redis;
    private readonly RedisOptions _redisOptions;
    private readonly ILogger<RegisterService> _logger;
    private readonly ISensitiveDataProtector _sensitiveDataProtector;

    public RegisterService(
        AppDbContext db,
        IPortOneIvVerifyService portOneIvService,
        IDatabase redis,
        RedisOptions redisOptions,
        ISensitiveDataProtector sensitiveDataProtector,
        ILogger<RegisterService> logger)
    {
        _db = db;
        _portOneIvService = portOneIvService;
        _redis = redis;
        _redisOptions = redisOptions;
        _sensitiveDataProtector = sensitiveDataProtector;
        _logger = logger;
    }

    // 본인인증 검증
    public async Task<(bool Success, PortOneVerifiedIdentityResponse? VerifiedIdentity, string? ErrorCode)> VerifyIvAsync(
        PortOneIvVerifyRequest request,
        CancellationToken ct = default)
    {
        // 본인인증 검증 요청
        var (success, identityVerification, errorCode) = await _portOneIvService.VerifyAsync(request.IdentityVerificationId, ct);
        if (!success)
            return (false, null, errorCode ?? "AUTH_VERIFY_IV_FAILED");

        // 검증 결과가 없으면 오류 반환
        if (identityVerification is null)
            return (false, null, "AUTH_VERIFY_IV_INVALID_RESPONSE");

        // 검증 결과를 프로필로 변환
        var verifiedProfile = ToVerifiedIdentityProfile(identityVerification.Value);

        var normalizedVerifiedBirthday = NormalizeBirthday(verifiedProfile.Birthday);
        var normalizedVerifiedPhone = NormalizePhone(verifiedProfile.PhoneNumber);
        var normalizedVerifiedGender = NormalizeGender(verifiedProfile.Gender);
        var normalizedUniqueKey = NormalizeUniqueKey(verifiedProfile.UniqueKey);
        if (normalizedVerifiedBirthday is null
            || normalizedVerifiedPhone is null
            || normalizedVerifiedGender is null
            || normalizedUniqueKey is null
            || string.IsNullOrWhiteSpace(verifiedProfile.Name))
            return (false, null, "AUTH_VERIFY_IV_INVALID_RESPONSE");

        var normalizedName = verifiedProfile.Name.Trim();
        var verificationToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        var cacheKey = BuildVerificationCacheKey(_redisOptions.KeyPrefix, verificationToken);
        await _redis.StringSetAsync(
            cacheKey,
            JsonSerializer.Serialize(new VerifiedIdentityCache(
                request.IdentityVerificationId,
                _sensitiveDataProtector.Protect(normalizedName),
                normalizedVerifiedGender,
                normalizedVerifiedBirthday,
                _sensitiveDataProtector.Protect(normalizedUniqueKey),
                _sensitiveDataProtector.Protect(normalizedVerifiedPhone),
                _sensitiveDataProtector.Protect(TryGetString(identityVerification.Value, "di", "DI")),
                DateTime.UtcNow,
                0)),
            TimeSpan.FromMinutes(VerificationTtlMinutes));

        var response = new PortOneVerifiedIdentityResponse(
            verificationToken,
            normalizedName,
            normalizedVerifiedGender,
            normalizedVerifiedBirthday,
            normalizedUniqueKey,
            normalizedVerifiedPhone);

        _logger.LogInformation("Identity verification succeeded and token was issued.");

        return (true, response, null);
    }

    /// <summary>
    /// 최종 회원가입 요청
    /// </summary>
    /// <param name="request">회원가입 요청 정보</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>회원가입 결과</returns>
    public async Task<(bool Success, string? ErrorCode)> RegisterAsync(
        RegisterRequest request,
        CancellationToken ct = default)
    {
        var cacheKey = BuildVerificationCacheKey(_redisOptions.KeyPrefix, request.VerificationToken);
        var cachedRaw = await _redis.StringGetAsync(cacheKey);
        if (cachedRaw.IsNullOrEmpty)
            return (false, "AUTH_REGISTER_INVALID_REQUEST");

        var cached = JsonSerializer.Deserialize<VerifiedIdentityCache>(cachedRaw.ToString());
        if (cached is null)
            return (false, "AUTH_REGISTER_INVALID_RESPONSE");

        var cachedName = _sensitiveDataProtector.Unprotect(cached.Name);
        var cachedUniqueKey = _sensitiveDataProtector.Unprotect(cached.UniqueKey);
        var cachedPhone = _sensitiveDataProtector.Unprotect(cached.PhoneNumber);
        var cachedDi = _sensitiveDataProtector.Unprotect(cached.Di);
        if (cachedName is null || cachedUniqueKey is null || cachedPhone is null)
            return (false, "AUTH_REGISTER_INVALID_RESPONSE");

        var normalizedRequest = NormalizeRegisterRequest(request);
        if (!normalizedRequest.IsValid)
            return (false, "AUTH_REGISTER_INVALID_REQUEST");

        if (!IsSameVerifiedIdentity(
                cachedName,
                cached.Gender,
                cached.Birthday,
                cachedUniqueKey,
                cachedPhone,
                normalizedRequest))
        {
            var attempts = await IncrementFailedAttemptAsync(cacheKey, cached);
            return attempts >= MaxRegisterAttempts
                ? (false, "AUTH_VERIFY_IV_RETRY_EXCEEDED")
                : (false, "AUTH_REGISTER_INVALID_RESPONSE");
        }

        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            return (false, "AUTH_EMAIL_ALREADY_EXISTS");

        var now = DateTime.UtcNow;
        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Name = cachedName,
            Phone = cachedPhone,
            CreatedAt = now
        };

        _db.Users.Add(user);

        var verification = new UserVerification
        {
            UserId = user.Id,
            Provider = VerificationProvider.Pass,
            Status = VerificationStatus.Verified,
            VerifiedAt = now,
            Name = _sensitiveDataProtector.Protect(cachedName),
            BirthDate = DateOnly.TryParseExact(cached.Birthday, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var birthDate)
                ? birthDate
                : null,
            Gender = ToVerificationGender(cached.Gender),
            PhoneNumber = _sensitiveDataProtector.Protect(cachedPhone),
            Ci = _sensitiveDataProtector.Protect(cachedUniqueKey),
            Di = _sensitiveDataProtector.Protect(cachedDi),
            ProviderTransactionId = cached.IdentityVerificationId,
            CreatedAt = now
        };

        _db.UserVerifications.Add(verification);
        await _db.SaveChangesAsync(ct);
        await _redis.KeyDeleteAsync(cacheKey);
        return (true, null);
    }

    private static VerifiedIdentityProfile ToVerifiedIdentityProfile(JsonElement identityVerification)
    {
        return new VerifiedIdentityProfile(
            Name: TryGetString(identityVerification, "name", "fullName"),
            Gender: TryGetString(identityVerification, "gender", "sex"),
            Birthday: TryGetString(identityVerification, "birthday", "birthDate", "birthdate", "dateOfBirth"),
            UniqueKey: TryGetString(identityVerification, "unique_key", "uniqueKey", "ci", "CI"),
            PhoneNumber: TryGetString(identityVerification, "phoneNumber", "phone", "phoneNo", "mobile"));
    }

    private static string? TryGetString(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out var value))
                return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        }

        return null;
    }

    private static string BuildVerificationCacheKey(string prefix, string verificationToken)
    {
        var resolvedPrefix = string.IsNullOrWhiteSpace(prefix) ? "spotops" : prefix.Trim();
        return $"{resolvedPrefix}:auth:iv:{verificationToken}";
    }

    private static string? NormalizeBirthday(string? rawBirthday)
    {
        if (string.IsNullOrWhiteSpace(rawBirthday))
            return null;

        var trimmed = rawBirthday.Trim();
        if (!DateOnly.TryParseExact(trimmed, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return null;

        return parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static string? NormalizePhone(string? rawPhone)
    {
        if (string.IsNullOrWhiteSpace(rawPhone))
            return null;

        var digits = new string(rawPhone.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }

    private static string? NormalizeUniqueKey(string? rawUniqueKey)
    {
        if (string.IsNullOrWhiteSpace(rawUniqueKey))
            return null;

        return rawUniqueKey.Trim().ToUpperInvariant();
    }

    private static string? NormalizeGender(string? rawGender)
    {
        if (string.IsNullOrWhiteSpace(rawGender))
            return null;

        return rawGender.Trim().ToUpperInvariant() switch
        {
            "M" or "MALE" or "MAN" => "MALE",
            "F" or "FEMALE" or "WOMAN" => "FEMALE",
            "U" or "UNKNOWN" => "UNKNOWN",
            _ => null
        };
    }

    private static VerificationGender? ToVerificationGender(string? normalizedGender)
    {
        return normalizedGender switch
        {
            "MALE" => VerificationGender.Male,
            "FEMALE" => VerificationGender.Female,
            "UNKNOWN" => VerificationGender.Unknown,
            _ => null
        };
    }

    private static NormalizedRegisterRequest NormalizeRegisterRequest(RegisterRequest request)
    {
        var name = string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim();
        return new NormalizedRegisterRequest(
            name,
            NormalizeGender(request.Gender),
            NormalizeBirthday(request.Birthday),
            NormalizeUniqueKey(request.UniqueKey),
            NormalizePhone(request.Phone));
    }

    private static bool IsSameVerifiedIdentity(
        string? cachedName,
        string? cachedGender,
        string? cachedBirthday,
        string? cachedUniqueKey,
        string? cachedPhone,
        NormalizedRegisterRequest request)
    {
        return string.Equals(cachedName, request.Name, StringComparison.Ordinal)
            && string.Equals(cachedGender, request.Gender, StringComparison.Ordinal)
            && string.Equals(cachedBirthday, request.Birthday, StringComparison.Ordinal)
            && string.Equals(cachedUniqueKey, request.UniqueKey, StringComparison.Ordinal)
            && string.Equals(cachedPhone, request.Phone, StringComparison.Ordinal);
    }

    private async Task<int> IncrementFailedAttemptAsync(string cacheKey, VerifiedIdentityCache cached)
    {
        var nextAttempts = cached.FailedAttempts + 1;
        var ttl = await _redis.KeyTimeToLiveAsync(cacheKey);
        var expiration = ttl is null || ttl <= TimeSpan.Zero
            ? TimeSpan.FromMinutes(VerificationTtlMinutes)
            : ttl.Value;

        var updated = cached with { FailedAttempts = nextAttempts };
        await _redis.StringSetAsync(cacheKey, JsonSerializer.Serialize(updated), expiration);
        return nextAttempts;
    }

    private sealed record VerifiedIdentityCache(
        string IdentityVerificationId,
        string? Name,
        string? Gender,
        string? Birthday,
        string? UniqueKey,
        string? PhoneNumber,
        string? Di,
        DateTime VerifiedAtUtc,
        int FailedAttempts);

    private sealed record VerifiedIdentityProfile(
        string? Name,
        string? Gender,
        string? Birthday,
        string? UniqueKey,
        string? PhoneNumber);

    private sealed record NormalizedRegisterRequest(
        string? Name,
        string? Gender,
        string? Birthday,
        string? UniqueKey,
        string? Phone)
    {
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(Name)
            && !string.IsNullOrWhiteSpace(Gender)
            && !string.IsNullOrWhiteSpace(Birthday)
            && !string.IsNullOrWhiteSpace(UniqueKey)
            && !string.IsNullOrWhiteSpace(Phone);
    }
}
