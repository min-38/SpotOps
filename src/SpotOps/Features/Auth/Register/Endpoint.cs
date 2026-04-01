using Microsoft.Extensions.Options;
using SpotOps.Contracts;
using SpotOps.Features.Auth;
using SpotOps.Infrastructure.PortOne;

namespace SpotOps.Features.Auth.Register;

public static partial class RegisterEndpoint
{
    public static void Map(RouteGroupBuilder auth)
    {
        auth.MapGet("/iv/config", GetIvConfigAsync).AllowAnonymous();
        auth.MapPost("/iv/verify", VerifyIvAsync).AllowAnonymous();
        auth.MapPost("/register", RegisterAsync).AllowAnonymous();
    }

    // PortOne storeId, verifyChannelId 조회
    public static IResult GetIvConfigAsync(IOptions<PortOneOptions> portOne)
    {
        var o = portOne.Value;
        return Results.Json(
            ApiResponse<object?>.Ok(new PortOneIvConfigResponse(o.StoreId, o.VerifyChannelId)));
    }
    
    // PortOne 아이디 검증
    public static async Task<IResult> VerifyIvAsync(
        PortOneIvVerifyRequest request,
        IRegisterService registerService,
        CancellationToken ct)
    {
        var isValid = RegisterValidation.IvValidate(request);
        if (!isValid)
            return Results.Json(
                ApiResponse<object?>.Fail("AUTH_VERIFY_IV_INVALID_REQUEST"),
                statusCode: StatusCodes.Status400BadRequest);

        var (success, verifiedIdentity, errorCode) = await registerService.VerifyIvAsync(request, ct);
        if (!success)
            return Results.Json(
                ApiResponse<object?>.Fail(errorCode ?? "AUTH_VERIFY_IV_FAILED"),
                statusCode: StatusCodes.Status400BadRequest);

        if (verifiedIdentity is null)
            return Results.Json(
                ApiResponse<object?>.Fail("AUTH_VERIFY_IV_INVALID_RESPONSE"),
                statusCode: StatusCodes.Status400BadRequest);

        return Results.Json(ApiResponse<PortOneVerifiedIdentityResponse>.Ok(verifiedIdentity));
    }

    public static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        IRegisterService registerService,
        CancellationToken ct)
    {
        var isValid = RegisterValidation.RegisterValidate(request);
        if (!isValid)
            return Results.Json(
                ApiResponse<object?>.Fail("AUTH_VALIDATION_FAILED"),
                statusCode: StatusCodes.Status400BadRequest);

        var (success, errorCode) = await registerService.RegisterAsync(request, ct);
        if (!success)
            return Results.Json(
                ApiResponse<object?>.Fail(errorCode ?? "AUTH_REGISTER_FAILED"),
                statusCode: StatusCodes.Status400BadRequest);

        return Results.Json(ApiResponse<object?>.Ok(null), statusCode: StatusCodes.Status201Created);
    }
}
