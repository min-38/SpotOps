namespace SpotOps.Features.Auth.Register;

public sealed record PortOneIvConfigResponse(
    string StoreId,
    string VerifyChannelId);

public sealed record PortOneVerifiedIdentityResponse(
    string VerificationToken,
    string? Name,
    string? Gender,
    string? Birthday,
    string? UniqueKey,
    string? PhoneNumber);