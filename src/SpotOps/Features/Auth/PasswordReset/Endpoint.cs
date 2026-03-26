using SpotOps.Contracts;

namespace SpotOps.Features.Auth.PasswordReset;

public static class PasswordResetEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/auth/password")
            .WithTags("Auth");

        group.MapPost("/forgot", ForgotAsync).AllowAnonymous();
        group.MapPost("/reset", ResetAsync).AllowAnonymous();
    }

    private static async Task<IResult> ForgotAsync(
        ForgotPasswordRequest body,
        PasswordResetService service,
        CancellationToken cancellationToken)
    {
        var (ok, code, message) = await service.RequestAsync(body.Email, cancellationToken);
        if (!ok)
            return Results.Json(
                ApiResponse<object?>.Fail(code ?? "PASSWORD_RESET_RATE_LIMITED", message),
                statusCode: StatusCodes.Status429TooManyRequests);

        // 계정 유무 노출 방지: 항상 성공 응답
        return Results.Json(ApiResponse<object?>.Ok(null));
    }

    private static async Task<IResult> ResetAsync(
        ResetPasswordRequest body,
        PasswordResetService service,
        CancellationToken cancellationToken)
    {
        var (ok, code, message) = await service.ResetAsync(body.Token, body.NewPassword, cancellationToken);
        if (!ok)
            return Results.Json(
                ApiResponse<object?>.Fail(code ?? "PASSWORD_RESET_FAILED", message),
                statusCode: StatusCodes.Status400BadRequest);

        return Results.Json(ApiResponse<object?>.Ok(null));
    }
}

