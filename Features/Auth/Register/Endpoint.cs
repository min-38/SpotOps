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
            return Results.Json(new { success = false, error = emailError }, statusCode: StatusCodes.Status400BadRequest);

        return Results.Json(new { success = true }, statusCode: StatusCodes.Status201Created);
    }
}
