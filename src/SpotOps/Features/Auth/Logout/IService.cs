namespace SpotOps.Features.Auth.Logout;

public interface ILogoutService
{
    Task RevokeRefreshTokenAsync(Guid userId, string refreshToken, CancellationToken ct = default);
}
