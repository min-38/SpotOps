using SpotOps.Models;

namespace SpotOps.Features.Me.Profile;

public sealed record MyProfileDto(
    Guid Id,
    string Email,
    string Name,
    string? Phone,
    DateTime? PhoneVerifiedAt,
    UserRole Role,
    DateTime CreatedAt);

public sealed record UpdateMyProfileRequest(
    string Name,
    string? Phone);

public sealed record SendPhoneOtpRequest(string Phone);

public sealed record VerifyPhoneOtpRequest(string Code);

