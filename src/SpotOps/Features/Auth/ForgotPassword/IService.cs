namespace SpotOps.Features.Auth.ForgotPassword;

public interface IForgotPasswordService
{
    Task<(bool Success, string? ErrorCode)> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);
}