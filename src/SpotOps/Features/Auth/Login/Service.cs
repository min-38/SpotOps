using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SpotOps.Data;
using SpotOps.Models;
using System.Security.Cryptography;
using System.Net.Mail;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SpotOps.Features.Auth.Login;

public sealed class LoginService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;

    public LoginService(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
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

    public (string AccessToken, long ExpiresInSeconds) CreateAccessToken(User user)
    {
        var issuer = _configuration["JWT_ISSUER"] ?? "spotops";
        var audience = _configuration["JWT_AUDIENCE"] ?? "spotops-client";
        var secret = _configuration["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET is required.");
        var expiresInSeconds = long.TryParse(_configuration["JWT_ACCESS_TOKEN_EXPIRES_SECONDS"], out var rawExpires)
            ? rawExpires
            : 60 * 60 * 2;
        if (expiresInSeconds <= 0)
            expiresInSeconds = 60 * 60 * 2;

        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: now.AddSeconds(expiresInSeconds),
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresInSeconds);
    }

    public async Task<LoginTokenDto> CreateTokenPairAsync(User user, CancellationToken cancellationToken = default)
    {
        var (accessToken, expiresInSeconds) = CreateAccessToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, cancellationToken);
        var refreshTtlDays = GetRefreshTokenTtlDays();

        return new LoginTokenDto(
            accessToken,
            "Bearer",
            expiresInSeconds,
            refreshToken,
            refreshTtlDays * 24 * 60 * 60);
    }

    public async Task<(User? User, LoginTokenDto? Tokens, string? ErrorCode, string? ErrorMessage)> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var rawToken = (refreshToken ?? string.Empty).Trim();
        if (rawToken.Length < 16)
            return (null, null, "AUTH_REFRESH_TOKEN_INVALID", "리프레시 토큰이 올바르지 않아요.");

        var now = DateTime.UtcNow;
        var tokenHash = Hash(rawToken);
        var existing = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (existing is null || existing.RevokedAt is not null || existing.ExpiresAt <= now)
            return (null, null, "AUTH_REFRESH_TOKEN_INVALID", "리프레시 토큰이 올바르지 않아요.");

        existing.RevokedAt = now;
        var newToken = await CreateRefreshTokenAsync(existing.UserId, cancellationToken);
        var (accessToken, expiresInSeconds) = CreateAccessToken(existing.User);
        var refreshTtlDays = GetRefreshTokenTtlDays();

        await _db.SaveChangesAsync(cancellationToken);

        return (
            existing.User,
            new LoginTokenDto(
                accessToken,
                "Bearer",
                expiresInSeconds,
                newToken,
                refreshTtlDays * 24 * 60 * 60),
            null,
            null);
    }

    public async Task RevokeRefreshTokenAsync(Guid userId, string refreshToken, CancellationToken cancellationToken = default)
    {
        var rawToken = (refreshToken ?? string.Empty).Trim();
        if (rawToken.Length < 16)
            return;

        var tokenHash = Hash(rawToken);
        var existing = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.TokenHash == tokenHash, cancellationToken);

        if (existing is null || existing.RevokedAt is not null)
            return;

        existing.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> CreateRefreshTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            TokenHash = Hash(token),
            ExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenTtlDays())
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(cancellationToken);
        return token;
    }

    private int GetRefreshTokenTtlDays()
    {
        if (!int.TryParse(_configuration["JWT_REFRESH_TOKEN_EXPIRES_DAYS"], out var days) || days <= 0)
            return 14;
        return days;
    }

    private static string Hash(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes);
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
