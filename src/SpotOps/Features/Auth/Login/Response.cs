namespace SpotOps.Features.Auth.Login;

public sealed record LoginResponse(
    Guid Id,
    string Email,
    string Name,
    JWTResponse Tokens
);

public sealed record JWTResponse(
    string AccessToken,
    string TokenType,
    long ExpiresInSeconds,
    string RefreshToken,
    long RefreshTokenExpiresInSeconds);
