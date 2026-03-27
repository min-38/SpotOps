using SpotOps.Contracts;
using System.Security.Claims;

namespace SpotOps.Features.Auth.Login;

public static class LoginEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login",
            async (LoginDto body, LoginService login, CancellationToken cancellationToken) =>
            {
                var (user, error) = await login.ValidateAsync(body, cancellationToken);
                if (error is not null || user is null)
                    return Results.Json(
                        ApiResponse<LoginUserDto>.Fail("AUTH_INVALID_CREDENTIALS", error),
                        statusCode: StatusCodes.Status401Unauthorized);

                var tokens = await login.CreateTokenPairAsync(user, cancellationToken);

                var payload = new LoginUserDto(
                    user.Id,
                    user.Email,
                    user.Name,
                    user.Role.ToString(),
                    tokens);
                return Results.Json(ApiResponse<LoginUserDto>.Ok(payload));
            }).AllowAnonymous();

        group.MapPost("/refresh", async (RefreshTokenRequestDto body, LoginService login, CancellationToken cancellationToken) =>
        {
            var (user, tokens, errorCode, errorMessage) = await login.RefreshAsync(body.RefreshToken, cancellationToken);
            if (user is null || tokens is null)
                return Results.Json(
                    ApiResponse<LoginUserDto>.Fail(errorCode ?? "AUTH_REFRESH_TOKEN_INVALID", errorMessage),
                    statusCode: StatusCodes.Status401Unauthorized);

            var payload = new LoginUserDto(
                user.Id,
                user.Email,
                user.Name,
                user.Role.ToString(),
                tokens);
            return Results.Json(ApiResponse<LoginUserDto>.Ok(payload));
        }).AllowAnonymous();

        group.MapPost("/logout", async (LogoutRequestDto body, LoginService login, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            if (Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
                && !string.IsNullOrWhiteSpace(body.RefreshToken))
            {
                await login.RevokeRefreshTokenAsync(userId, body.RefreshToken, cancellationToken);
            }

            return Results.Json(ApiResponse<object?>.Ok(null));
        }).RequireAuthorization();
    }
}
