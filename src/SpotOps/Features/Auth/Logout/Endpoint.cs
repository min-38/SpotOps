using System.Security.Claims;
using SpotOps.Contracts;
using SpotOps.Features.Auth.Logout;

namespace SpotOps.Features.Auth.Logout;

public static partial class LogoutEndpoint
{
    public static void Map(RouteGroupBuilder auth)
    {
        auth.MapPost("/logout", LogoutAsync).RequireAuthorization();
    }

    public static async Task<IResult> LogoutAsync(
        LogoutRequest request,
        ILogoutService logoutService,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        if (Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            && !string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            await logoutService.RevokeRefreshTokenAsync(userId, request.RefreshToken, ct);
        }

        // 유효하지 않은 토큰이든 취소된 토큰이든, 토큰이 없든 무조건 성공
        // 애초에 유효하지 않은 토큰으로 접근했다는 것 자체가 올바른 접근이 아님
        // 토큰이 없다는 것은 클라이언트 측에서 토큰을 제거한 것이므로 성공 처리
        return Results.Json(ApiResponse<object?>.Ok(null));
    }
}
