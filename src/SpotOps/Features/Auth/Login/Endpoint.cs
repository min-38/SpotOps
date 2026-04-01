using SpotOps.Contracts;

namespace SpotOps.Features.Auth.Login;

public static partial class LoginEndpoint
{
    public static void Map(RouteGroupBuilder auth)
    {
        auth.MapPost("/login", LoginAsync).AllowAnonymous();
        auth.MapPost("/refresh", RefreshAsync).AllowAnonymous();
    }

    public static async Task<IResult> LoginAsync(
        LoginRequest request,
        ILoginService loginService,
        CancellationToken ct)
    {
        if (!LoginValidation.ValidateLoginRequest(request))
            return Results.Json(
                ApiResponse<LoginResponse>.Fail("VALIDATION_FAILED"),
                statusCode: StatusCodes.Status400BadRequest);

        var user = await loginService.ValidateAsync(request.Email, request.Password, ct);
        if (user is null)
            return Results.Json(
                ApiResponse<LoginResponse>.Fail("AUTH_INVALID_CREDENTIALS"),
                statusCode: StatusCodes.Status401Unauthorized);

        var tokens = await loginService.CreateTokenPairAsync(user, ct);
        var payload = new LoginResponse(
            user.Id,
            user.Email,
            user.Name,
            new JWTResponse(
                tokens.AccessToken,
                "Bearer",
                tokens.ExpiresInSeconds,
                tokens.RefreshToken,
                tokens.RefreshTokenExpiresInSeconds
            )
        );

        return Results.Json(ApiResponse<LoginResponse>.Ok(payload));
    }

    public static async Task<IResult> RefreshAsync(
        RefreshTokenRequest request,
        ILoginService loginService,
        CancellationToken ct)
    {
        if (!LoginValidation.ValidateRefreshTokenRequest(request))
            return Results.Json(
                ApiResponse<LoginResponse>.Fail("VALIDATION_FAILED"),
                statusCode: StatusCodes.Status400BadRequest);

        var (user, tokens, errorCode) = await loginService.RefreshTokenAsync(request.RefreshToken, ct);
        if (user is null || tokens is null)
            return Results.Json(
                ApiResponse<LoginResponse>.Fail(errorCode ?? "AUTH_REFRESH_TOKEN_INVALID"),
                statusCode: StatusCodes.Status401Unauthorized);

        var payload = new LoginResponse(user.Id, user.Email, user.Name, tokens);
        return Results.Json(ApiResponse<LoginResponse>.Ok(payload));
    }
}
