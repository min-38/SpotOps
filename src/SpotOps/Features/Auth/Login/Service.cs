using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SpotOps.Data;
using SpotOps.Models;
using SpotOps.Features.Auth.JWT;

namespace SpotOps.Features.Auth.Login;

public sealed partial class LoginService : ILoginService
{
    private readonly AppDbContext _db;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<LoginService> _logger;

    public LoginService(AppDbContext db, IJwtTokenService jwtTokenService, ILogger<LoginService> logger)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    /// <summary>
    /// 로그인 유저 검증
    /// </summary>
    /// <param name="dto">로그인 요청 정보</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>유저 정보</returns>
    public async Task<User?> ValidateAsync(string email, string password, CancellationToken ct = default)
    {
        // 이미 endpoint에서 검증을 했기 때문에 유효성 검사는 여기서 하지 않는다.
        
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            _logger.LogWarning("Invalid email or password: {Email}", email);
            return null;
        }

        _logger.LogInformation("User validated: {Email}", email);
        return user;
    }

    /// <summary>
    /// 토큰 생성
    /// </summary>
    /// <param name="user">유저 정보</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>토큰 정보</returns>
    public async Task<JWTResponse> CreateTokenPairAsync(User user, CancellationToken ct = default)
    {
        if (user is null)
        {
            _logger.LogWarning("User is null: {User}", user);
            throw new ArgumentNullException(nameof(user));
        }
            
        var (accessToken, expiresInSeconds) = _jwtTokenService.CreateAccessToken(user);
        if (accessToken is null)
        {
            _logger.LogWarning("Failed to create access token: {User}", user);
            throw new InvalidOperationException("Failed to create access token");
        }

        var refreshToken = _jwtTokenService.CreateRefreshToken();
        var refreshTokenExpiresInSeconds = _jwtTokenService.GetRefreshTokenTtlDays();

        _logger.LogInformation("Token pair created for user {UserId}: {AccessToken}, {RefreshToken}", user.Id, accessToken, refreshToken);

        return new JWTResponse(accessToken, "Bearer", expiresInSeconds, refreshToken, refreshTokenExpiresInSeconds);
    }

    /// <summary>
    /// refresh token 갱신
    /// </summary>
    /// <param name="refreshToken">refresh token</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>유저 정보, 토큰 정보, 에러 코드</returns>
    public async Task<(User? User, JWTResponse? Tokens, string? ErrorCode)> RefreshTokenAsync(
        string rawToken, // 기존 refresh token
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var tokenHash = _jwtTokenService.HashRefresh(rawToken);

        // 존재하지 않은 refresh token이거나, 취소/만료된 토큰이면 에러 반환
        var existing = await _db.RefreshTokens
            .Include(t => t.User)
            .Where(t => t.TokenHash == tokenHash)
            .FirstOrDefaultAsync(ct);
        if (existing is null || existing.RevokedAt is not null || existing.ExpiresAt <= now)
        {
            _logger.LogWarning("Invalid refresh token: {RefreshToken}", rawToken);
            return (null, null, "AUTH_REFRESH_TOKEN_INVALID");
        }

        existing.RevokedAt = now;

        // 새로운 refresh token 생성 후 DB에 저장
        var newRefreshToken = _jwtTokenService.CreateRefreshToken();
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = existing.User.Id,
            TokenHash = _jwtTokenService.HashRefresh(newRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtTokenService.GetRefreshTokenTtlDays())
        });
        await _db.SaveChangesAsync(ct);

        var (accessToken, expiresInSeconds) = _jwtTokenService.CreateAccessToken(existing.User);
        var refreshTokenExpiresInSeconds = _jwtTokenService.GetRefreshTokenTtlDays() * 24 * 60 * 60;

        _logger.LogInformation("Refresh token refreshed for user {UserId}: {RefreshToken}", existing.User.Id, newRefreshToken);

        return (
            existing.User,
            new JWTResponse(
                accessToken,
                "Bearer",
                expiresInSeconds,
                newRefreshToken,
                refreshTokenExpiresInSeconds),
            null);
    }
}
