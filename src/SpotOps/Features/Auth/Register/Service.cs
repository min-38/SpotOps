using Microsoft.EntityFrameworkCore;
using SpotOps.Data;
using SpotOps.Models;

namespace SpotOps.Features.Auth.Register;

public sealed class RegisterService
{
    private readonly AppDbContext _db;

    public RegisterService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> RegisterAsync(
        RegisterDto dto,
        CancellationToken cancellationToken = default)
    {
        var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();
        var name = (dto.Name ?? string.Empty).Trim();
        var phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();

        if (await _db.Users.AnyAsync(u => u.Email == email, cancellationToken))
            return (false, "AUTH_EMAIL_ALREADY_EXISTS", "이미 사용 중인 이메일이에요.");

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Name = name,
            Phone = phone,
            Role = UserRole.Buyer
        };

        _db.Users.Add(user);

        await _db.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }
}
