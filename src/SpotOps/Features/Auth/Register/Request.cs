namespace SpotOps.Features.Auth;

public sealed record PortOneIvVerifyRequest(string IdentityVerificationId);

public sealed record RegisterRequest(
    string VerificationToken,
    string Name,
    string Gender,
    string Birthday,
    string UniqueKey,
    string Phone,
    string Email,
    string Password,
    string PasswordConfirmation
);
