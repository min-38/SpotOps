using SpotOps.Contracts;

namespace SpotOps.Features.Auth.Register;

public static class RegisterEndpoint
{
    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/api/auth").WithTags("Auth");
        g.MapPost("/register", RegisterAsync).AllowAnonymous();
    }

    private static async Task<IResult> RegisterAsync(
        RegisterDto body,
        RegisterService register,
        CancellationToken cancellationToken)
    {
        var (success, emailError) = await register.RegisterAsync(body, cancellationToken);
        if (!success)
            return Results.Json(
                ApiResponse<object?>.Fail("AUTH_EMAIL_ALREADY_EXISTS", emailError),
                statusCode: StatusCodes.Status400BadRequest);

        return Results.Json(ApiResponse<object?>.Ok(null), statusCode: StatusCodes.Status201Created);
    }
}
