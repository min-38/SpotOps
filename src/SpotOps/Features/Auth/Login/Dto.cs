namespace SpotOps.Features.Auth.Login;

public sealed record LoginDto(string Email, string Password);

public sealed record LoginUserDto(Guid Id, string Email, string Name, string Role);
