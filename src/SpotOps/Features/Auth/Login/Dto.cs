namespace SpotOps.Features.Auth.Login;

public sealed record LoginDto(string Email, string Password);

public sealed record LoginTokenDto(
    string AccessToken,
    string TokenType,
    long ExpiresInSeconds,
    string RefreshToken,
    long RefreshTokenExpiresInSeconds);

public sealed record LoginUserDto(
    Guid Id,
    string Email,
    string Name,
    string Role,
    LoginTokenDto Tokens);

public sealed record RefreshTokenRequestDto(string RefreshToken);

public sealed record LogoutRequestDto(string? RefreshToken);
