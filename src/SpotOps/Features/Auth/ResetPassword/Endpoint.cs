using SpotOps.Contracts;

namespace SpotOps.Features.Auth.ResetPassword;

public static partial class ResetPasswordEndpoint
{
    public static void Map(RouteGroupBuilder auth)
    {
        auth.MapGet("/reset-token-validate", ValidateTokenAsync).AllowAnonymous();
        auth.MapPost("/reset-password", ResetPasswordAsync).AllowAnonymous();
    }

    public static async Task<IResult> ValidateTokenAsync(
        string token,
        IResetPasswordService resetPasswordService,
        CancellationToken ct)
    {
        if (!ResetPasswordValidation.ResetTokenValidate(token))
            return Results.Json(
                ApiResponse<object?>.Fail("PASSWORD_RESET_TOKEN_INVALID"),
                statusCode: StatusCodes.Status400BadRequest);

        var (ok, code) = await resetPasswordService.ValidateResetTokenAsync(token, ct);
        if (!ok)
            return Results.Json(
                ApiResponse<object?>.Fail(code ?? "PASSWORD_RESET_TOKEN_INVALID"),
                statusCode: StatusCodes.Status400BadRequest);

        return Results.Json(ApiResponse<object?>.Ok(null));
    }

    public static async Task<IResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        IResetPasswordService resetPasswordService,
        CancellationToken ct)
    {
        if (!ResetPasswordValidation.ResetTokenValidate(request.Token))
            return Results.Json(
                ApiResponse<object?>.Fail("PASSWORD_RESET_TOKEN_INVALID"),
                statusCode: StatusCodes.Status400BadRequest);
        if (!ResetPasswordValidation.NewPasswordValidate(request))
            return Results.Json(
                ApiResponse<object?>.Fail("PASSWORD_RESET_NEW_PASSWORD_INVALID"),
                statusCode: StatusCodes.Status400BadRequest);

        var (ok, code) = await resetPasswordService.ResetPasswordAsync(request, ct);
        if (!ok)
            return Results.Json(
                ApiResponse<object?>.Fail(code ?? "PASSWORD_RESET_FAILED"),
                statusCode: StatusCodes.Status400BadRequest);

        return Results.Json(ApiResponse<object?>.Ok(null));
    }
}
