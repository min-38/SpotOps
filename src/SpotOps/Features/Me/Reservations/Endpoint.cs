using System.Security.Claims;
using SpotOps.Contracts;

namespace SpotOps.Features.Me.Reservations;

public static class MyReservationsEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/me")
            .WithTags("Me")
            .RequireAuthorization();

        group.MapGet("/reservations", ListAsync);
        group.MapPost("/reservations/{reservationId:guid}/cancel", CancelAsync);
    }

    private static async Task<IResult> ListAsync(
        MyReservationsService service,
        ClaimsPrincipal user,
        int? take,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Results.Json(
                ApiResponse<object?>.Fail("AUTH_UNAUTHORIZED", "Unauthorized."),
                statusCode: StatusCodes.Status401Unauthorized);

        var rows = await service.ListAsync(userId, take ?? 50, cancellationToken);
        return Results.Json(ApiResponse<IReadOnlyList<MyReservationDto>>.Ok(rows));
    }

    private static async Task<IResult> CancelAsync(
        Guid reservationId,
        MyReservationsService service,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Results.Json(
                ApiResponse<object?>.Fail("AUTH_UNAUTHORIZED", "Unauthorized."),
                statusCode: StatusCodes.Status401Unauthorized);

        var (result, code, message) = await service.CancelAsync(userId, reservationId, cancellationToken);
        if (result is null)
            return Results.Json(
                ApiResponse<object?>.Fail(code ?? "ME_RESERVATION_CANCEL_FAILED", message),
                statusCode: StatusCodes.Status400BadRequest);

        return Results.Json(ApiResponse<CancelReservationResultDto>.Ok(result));
    }
}

