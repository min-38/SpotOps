namespace SpotOps.Features.Auth.Login;

public sealed record LoginRequest(
    string Email,
    string Password);

public sealed record RefreshTokenRequest(string RefreshToken);
