using SpotOps.Contracts;

namespace SpotOps.Features.Auth.ForgotPassword;

public static partial class ForgotPasswordEndpoint
{
    public static void Map(RouteGroupBuilder auth)
    {
        auth.MapPost("/forgot-password", ForgotPasswordAsync).AllowAnonymous();
    }

    public static async Task<IResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        IForgotPasswordService forgotPasswordService,
        CancellationToken ct)
    {
        var isValid = ForgotPasswordValidation.ValidateForgotPasswordRequest(request);
        if (!isValid)
            return Results.Json(
                ApiResponse<object?>.Fail("AUTH_FORGOT_PASSWORD_INVALID_REQUEST"),
                statusCode: StatusCodes.Status400BadRequest);

        var (success, errorCode) = await forgotPasswordService.ForgotPasswordAsync(request, ct);
        if (!success)
            return Results.Json(
                ApiResponse<object?>.Fail(errorCode ?? "AUTH_FORGOT_PASSWORD_FAILED"),
                statusCode: StatusCodes.Status400BadRequest);

        // 계정 존재 여부 노출 방지를 위해 항상 동일 응답
        return Results.Json(ApiResponse<object?>.Ok(null));
    }
}
