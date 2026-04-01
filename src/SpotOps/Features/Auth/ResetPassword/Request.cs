namespace SpotOps.Features.Auth.ResetPassword;

public sealed record ResetPasswordRequest(string Token, string NewPassword, string NewPasswordConfirmation);
