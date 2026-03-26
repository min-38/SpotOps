using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SpotOps.Data;
using SpotOps.Models;
using System.Net.Mail;
using System.Security.Claims;

namespace SpotOps.Features.Auth.Login;

public sealed class LoginService
{
    private readonly AppDbContext _db;

    public LoginService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(User? User, string? ErrorMessage)> ValidateAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();
        var password = dto.Password ?? string.Empty;

        if (!IsValidEmail(email) || string.IsNullOrWhiteSpace(password))
            return (null, "이메일 또는 비밀번호가 올바르지 않아요.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return (null, "이메일 또는 비밀번호가 올바르지 않아요.");

        return (user, null);
    }

    public ClaimsPrincipal CreatePrincipal(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
