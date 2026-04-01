using SpotOps.Models;

namespace SpotOps.Features.Auth.JWT;

public interface IJwtTokenService
{
    (string AccessToken, long ExpiresInSeconds) CreateAccessToken(User user);

    /// <summary>저장용이 아닌, 클라이언트에 내려줄 opaque refresh token 문자열을 생성합니다.</summary>
    string CreateRefreshToken();

    int GetRefreshTokenTtlDays();

    string HashRefresh(string value);
}
