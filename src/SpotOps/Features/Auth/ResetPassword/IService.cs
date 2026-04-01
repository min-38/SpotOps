namespace SpotOps.Features.Auth.ResetPassword;

public interface IResetPasswordService
{
    Task<(bool Ok, string? ErrorCode)> ValidateResetTokenAsync(string token, CancellationToken ct = default);
    Task<(bool Ok, string? ErrorCode)> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
}