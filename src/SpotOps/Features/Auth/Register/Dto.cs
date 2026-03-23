using SpotOps.Models;

namespace SpotOps.Features.Auth.Register;

public sealed record RegisterDto(
    string Email,
    string Password,
    string Name,
    string? Phone,
    UserRole Role,
    string? BusinessNumber,
    string? CompanyName);
