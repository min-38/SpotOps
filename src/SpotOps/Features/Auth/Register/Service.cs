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
    public async Task<(bool Success, PortOneVerifiedIdentityResponse? VerifiedIdentity, string? ErrorCode, string? ExistingMaskedEmail)> VerifyIvAsync(
        PortOneIvVerifyRequest request,
        CancellationToken ct = default)
    {
        // 본인인증 검증 요청
        var (success, identityVerification, errorCode) = await _portOneIvService.VerifyAsync(request.IdentityVerificationId, ct);
        if (!success)
            return (false, null, errorCode ?? "AUTH_VERIFY_IV_FAILED", null);

        // 검증 결과가 없으면 오류 반환
        if (identityVerification is null)
        {
            _logger.LogInformation("identityVerification is null");
            return (false, null, "AUTH_VERIFY_IV_INVALID_RESPONSE", null);
        }

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
            return (false, null, "AUTH_VERIFY_IV_INVALID_RESPONSE", null);

        var existingMaskedEmail = await FindExistingMaskedEmailAsync(
            normalizedUniqueKey,
            verifiedProfile.Name,
            normalizedVerifiedBirthday,
            normalizedVerifiedPhone,
            ct);
        if (!string.IsNullOrWhiteSpace(existingMaskedEmail))
            return (false, null, "AUTH_REGISTER_ALREADY_EXISTS", existingMaskedEmail);

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

        return (true, response, null, null);
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
            return (false, "AUTH_VERIFY_IV_EXPIRED");

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
            CreatedAt = now,
            UpdatedAt = now
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
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.UserVerifications.Add(verification);
        await _db.SaveChangesAsync(ct);
        await _redis.KeyDeleteAsync(cacheKey);
        return (true, null);
    }

    private static VerifiedIdentityProfile ToVerifiedIdentityProfile(JsonElement identityVerification)
    {
        var source = identityVerification;
        if (identityVerification.TryGetProperty("verifiedCustomer", out var verifiedCustomer)
            && verifiedCustomer.ValueKind == JsonValueKind.Object)
        {
            source = verifiedCustomer;
        }

        return new VerifiedIdentityProfile(
            Name: TryGetString(source, "name", "fullName"),
            Gender: TryGetString(source, "gender", "sex"),
            Birthday: TryGetString(source, "birthday", "birthDate", "birthdate", "dateOfBirth"),
            UniqueKey: TryGetString(source, "unique_key", "uniqueKey", "ci", "CI", "id"),
            PhoneNumber: TryGetString(source, "phoneNumber", "phone", "phoneNo", "mobile"));
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

    private async Task<string?> FindExistingMaskedEmailAsync(
        string normalizedUniqueKey,
        string? verifiedName,
        string normalizedBirthday,
        string normalizedPhone,
        CancellationToken ct)
    {
        // 1. CI로 매칭되는 사용자 찾기
        // CI는 본인인증 결과에서 받은 고유 키
        var verificationRows = await _db.UserVerifications
            .AsNoTracking()
            .Where(v => v.Status == VerificationStatus.Verified && v.Ci != null)
            .Select(v => new { v.UserId, v.Ci })
            .ToListAsync(ct);

        Guid? matchedUserId = null;
        foreach (var row in verificationRows)
        {
            var ci = _sensitiveDataProtector.Unprotect(row.Ci);
            if (string.Equals(NormalizeUniqueKey(ci), normalizedUniqueKey, StringComparison.Ordinal))
            {
                matchedUserId = row.UserId;
                break;
            }
        }

        // CI로 매칭되는 사용자가 없으면 생년월일, 이름, 전화번호로 매칭되는 사용자 찾기
        if (matchedUserId is null)
        {
            if (DateOnly.TryParseExact(
                    normalizedBirthday,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedBirthDate))
            {
                var normalizedName = string.IsNullOrWhiteSpace(verifiedName)
                    ? null
                    : verifiedName.Trim();

                if (!string.IsNullOrWhiteSpace(normalizedName))
                {
                    var candidates = await (
                        from v in _db.UserVerifications.AsNoTracking()
                        join u in _db.Users.AsNoTracking() on v.UserId equals u.Id
                        where v.Status == VerificationStatus.Verified
                              && v.BirthDate == parsedBirthDate
                              && u.Name == normalizedName
                              && u.Phone != null
                        select new { u.Id, u.Phone, u.Email }
                    ).ToListAsync(ct);

                    foreach (var candidate in candidates)
                    {
                        if (string.Equals(NormalizePhone(candidate.Phone), normalizedPhone, StringComparison.Ordinal))
                        {
                            matchedUserId = candidate.Id;
                            break;
                        }
                    }
                }
            }
        }

        // 매칭되는 사용자가 없으면 null 반환
        if (matchedUserId is null)
            return null;

        // 매칭되는 사용자가 있다면 그 사용자가 가입했던 이메일을 반환
        // 단, 이메일을 마스킹하여 반환
        var email = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == matchedUserId.Value)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(ct);

        return string.IsNullOrWhiteSpace(email) ? null : MaskEmail(email);
    }

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 0)
            return "****";

        var local = email[..at];
        var domain = email[at..];
        const string mask = "******";

        var headLength = local.Length switch
        {
            <= 3 => 1,
            4 => 1,
            _ => 2
        };

        var tailLength = local.Length switch
        {
            >= 6 => 2,
            5 => 1,
            4 => 1,
            _ => 0
        };

        if (local.Length <= headLength)
            return $"{local[..1]}{mask}{domain}";

        var head = local[..headLength];
        var tail = tailLength > 0 ? local[^tailLength..] : string.Empty;
        return $"{head}{mask}{tail}{domain}";
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
