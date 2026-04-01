using SpotOps.Features.Auth.JWT;
using SpotOps.Models;

namespace SpotOps.Tests.Auth;

internal sealed class StubJwtTokenService : IJwtTokenService
{
    private readonly string? _fixedHash;

    public StubJwtTokenService()
    {
    }

    public StubJwtTokenService(string fixedHash)
    {
        _fixedHash = fixedHash;
    }

    public string HashResult { get; init; } = "hash";
    public string NewRefreshHash { get; init; } = "hash";
    public string NewRefreshToken { get; init; } = "new_refresh";

    public (string AccessToken, long ExpiresInSeconds) CreateAccessToken(User user) => ("access", 3600);
    public string CreateRefreshToken() => NewRefreshToken;
    public int GetRefreshTokenTtlDays() => 14;

    public string HashRefresh(string value)
    {
        if (_fixedHash is not null)
            return _fixedHash;

        return value == NewRefreshToken ? NewRefreshHash : HashResult;
    }
}
