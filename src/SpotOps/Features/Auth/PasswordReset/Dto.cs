namespace SpotOps.Features.Auth.PasswordReset;

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Token, string NewPassword);

