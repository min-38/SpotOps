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

    public async Task<(bool Success, string? EmailError)> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email, cancellationToken))
            return (false, "이미 사용 중인 이메일이에요.");

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Name = dto.Name,
            Phone = dto.Phone,
            Role = dto.Role
        };

        _db.Users.Add(user);

        if (dto.Role == UserRole.Organizer)
        {
            _db.Organizers.Add(new Organizer
            {
                UserId = user.Id,
                BusinessNumber = dto.BusinessNumber ?? "",
                CompanyName = dto.CompanyName ?? "",
                IsVerified = false
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }
}
