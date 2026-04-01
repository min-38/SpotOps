using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SpotOps.Models;

namespace SpotOps.Features.Auth.JWT;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// access token 생성
    /// </summary>
    /// <param name="user">유저 정보</param>
    /// <returns>access token, 만료 시간</returns>
    public (string AccessToken, long ExpiresInSeconds) CreateAccessToken(User user)
    {
        if (user is null)
            throw new ArgumentNullException(nameof(user));

        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Name)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddSeconds(_options.AccessTokenExpiresInSeconds),
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), _options.AccessTokenExpiresInSeconds);
    }

    /// <summary>
    /// DB에는 해시만 저장하고, 여기서는 전달용 opaque 토큰(랜덤 64바이트, Base64Url)을 만듭니다.
    /// </summary>
    public string CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Base64UrlEncoder.Encode(bytes);
    }

    public int GetRefreshTokenTtlDays()
    {
        return _options.RefreshTokenExpiresInDays > 0
            ? _options.RefreshTokenExpiresInDays
            : 14;
    }

    public string HashRefresh(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes);
    }
}
