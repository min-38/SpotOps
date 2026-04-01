using SpotOps.Features.Auth;

namespace SpotOps.Features.Auth.Register;

public interface IRegisterService
{
    Task<(bool Success, PortOneVerifiedIdentityResponse? VerifiedIdentity, string? ErrorCode, string? ExistingMaskedEmail)> VerifyIvAsync(
        PortOneIvVerifyRequest request,
        CancellationToken ct = default);

    Task<(bool Success, string? ErrorCode)> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
}