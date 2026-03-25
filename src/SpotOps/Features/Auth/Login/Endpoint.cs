using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

using SpotOps.Contracts;

namespace SpotOps.Features.Auth.Login;

public static class LoginEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login",
            async (LoginDto body, LoginService login, HttpContext http, CancellationToken cancellationToken) =>
            {
                var (user, error) = await login.ValidateAsync(body, cancellationToken);
                if (error is not null || user is null)
                    return Results.Json(
                        ApiResponse<LoginUserDto>.Fail("AUTH_INVALID_CREDENTIALS", error),
                        statusCode: StatusCodes.Status401Unauthorized);

                await http.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    login.CreatePrincipal(user));

                var payload = new LoginUserDto(user.Id, user.Email, user.Name, user.Role.ToString());
                return Results.Json(ApiResponse<LoginUserDto>.Ok(payload));
            }).AllowAnonymous();

        group.MapPost("/logout", async (HttpContext http) =>
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Json(ApiResponse<object?>.Ok(null));
        }).RequireAuthorization();
    }
}
