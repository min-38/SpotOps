namespace SpotOps.Features.Auth.Register;

public sealed record RegisterDto(
    string Email,
    string Password,
    string Name,
    string? Phone);
