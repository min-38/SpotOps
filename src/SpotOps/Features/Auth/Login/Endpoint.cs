using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

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
                    return Results.Json(new LoginApiResponse(false, null, error), statusCode: StatusCodes.Status401Unauthorized);

                await http.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    login.CreatePrincipal(user));

                return Results.Json(new LoginApiResponse(
                    true,
                    new UserApiResponse(user.Id, user.Email, user.Name, user.Role.ToString()),
                    null));
            }).AllowAnonymous();

        group.MapPost("/logout", async (HttpContext http) =>
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Json(new { success = true });
        }).RequireAuthorization();
    }

    private sealed record LoginApiResponse(bool Success, UserApiResponse? User, string? Error);

    private sealed record UserApiResponse(Guid Id, string Email, string Name, string Role);
}
