using System.Security.Claims;
using SpotOps.Contracts;

namespace SpotOps.Features.Me.Profile;

public static class MyProfileEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/me")
            .WithTags("Me")
            .RequireAuthorization();

        group.MapGet("/", GetAsync);
        group.MapPut("/", UpdateAsync);
        group.MapPost("/phone/send-otp", SendPhoneOtpAsync);
        group.MapPost("/phone/verify-otp", VerifyPhoneOtpAsync);
    }

    private static async Task<IResult> GetAsync(
        MyProfileService service,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Results.Json(
                ApiResponse<object?>.Fail("AUTH_UNAUTHORIZED", "Unauthorized."),
                statusCode: StatusCodes.Status401Unauthorized);

        var profile = await service.GetAsync(userId, cancellationToken);
        if (profile is null)
            return Results.Json(
                ApiResponse<object?>.Fail("ME_PROFILE_NOT_FOUND"),
                statusCode: StatusCodes.Status404NotFound);

        return Results.Json(ApiResponse<MyProfileDto>.Ok(profile));
    }

    private static async Task<IResult> UpdateAsync(
        UpdateMyProfileRequest body,
        MyProfileService service,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Results.Json(
                ApiResponse<object?>.Fail("AUTH_UNAUTHORIZED", "Unauthorized."),
                statusCode: StatusCodes.Status401Unauthorized);

        var (profile, code, message) = await service.UpdateAsync(userId, body, cancellationToken);
        if (profile is null)
            return Results.Json(
                ApiResponse<object?>.Fail(code ?? "ME_PROFILE_UPDATE_FAILED", message),
                statusCode: StatusCodes.Status400BadRequest);

        return Results.Json(ApiResponse<MyProfileDto>.Ok(profile));
    }

    private static async Task<IResult> SendPhoneOtpAsync(
        SendPhoneOtpRequest body,
        PhoneVerificationService verification,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Results.Json(
                ApiResponse<object?>.Fail("AUTH_UNAUTHORIZED", "Unauthorized."),
                statusCode: StatusCodes.Status401Unauthorized);

        var (ok, code, message) = await verification.SendOtpAsync(userId, body.Phone, cancellationToken);
        if (!ok)
        {
            var statusCode = code == "PHONE_OTP_RATE_LIMITED"
                ? StatusCodes.Status429TooManyRequests
                : StatusCodes.Status400BadRequest;
            return Results.Json(
                ApiResponse<object?>.Fail(code ?? "PHONE_OTP_SEND_FAILED", message),
                statusCode: statusCode);
        }

        return Results.Json(ApiResponse<object?>.Ok(null));
    }

    private static async Task<IResult> VerifyPhoneOtpAsync(
        VerifyPhoneOtpRequest body,
        PhoneVerificationService verification,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Results.Json(
                ApiResponse<object?>.Fail("AUTH_UNAUTHORIZED", "Unauthorized."),
                statusCode: StatusCodes.Status401Unauthorized);

        var (ok, code, message) = await verification.VerifyOtpAsync(userId, body.Code, cancellationToken);
        if (!ok)
            return Results.Json(
                ApiResponse<object?>.Fail(code ?? "PHONE_OTP_VERIFY_FAILED", message),
                statusCode: StatusCodes.Status400BadRequest);

        return Results.Json(ApiResponse<object?>.Ok(null));
    }
}

