using Microsoft.AspNetCore.Mvc;
using SpotOps.Models;
using System.Security.Claims;

namespace SpotOps.Features.Events.Reserve;

public static class ReserveEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/events/{eventId:guid}/reserve")
            .RequireAuthorization();

        group.MapPost("/", ReserveAsync);
        group.MapDelete("/", CancelAsync);
        group.MapGet("/", GetStatusAsync);
    }

    private static async Task<IResult> ReserveAsync(
        Guid eventId,
        [FromBody] ReserveRequestDto dto,
        ReserveService service,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var (reservation, error) = await service.ReserveAsync(eventId, userId, dto.SeatId, cancellationToken);

        if (error is not null)
            return Results.BadRequest(new { error });

        return Results.Ok(new ReserveResponseDto(reservation!.Id, reservation.ExpiresAt));
    }

    private static async Task<IResult> CancelAsync(
        Guid eventId,
        [FromQuery] Guid reservationId,
        ReserveService service,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var success = await service.CancelAsync(reservationId, userId, cancellationToken);

        return success ? Results.Ok() : Results.BadRequest(new { error = "취소할 수 없어요." });
    }

    private static async Task<IResult> GetStatusAsync(
        Guid eventId,
        ReserveService service,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var reservation = await service.GetStatusAsync(eventId, userId, cancellationToken);

        return reservation is null
            ? Results.NotFound()
            : Results.Ok(reservation);
    }
}
