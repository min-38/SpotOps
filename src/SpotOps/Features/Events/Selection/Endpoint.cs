using System.Security.Claims;
using SpotOps.Contracts;

namespace SpotOps.Features.Events.Selection;

public static class SelectionEndpoint
{
    public const string QueueSessionHeaderName = "X-Queue-Session";

    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/events/{eventId:guid}/selection")
            .WithTags("Events.Selection")
            .RequireAuthorization();

        group.MapGet("/layout", GetLayoutAsync);
        group.MapPost("/hold", HoldAsync);
        group.MapPost("/release", ReleaseAsync);
    }

    private static async Task<IResult> GetLayoutAsync(
        Guid eventId,
        HttpRequest request,
        ClaimsPrincipal user,
        SelectionService selection,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Results.Json(
                ApiResponse<object?>.Fail("AUTH_UNAUTHORIZED", "Unauthorized."),
                statusCode: StatusCodes.Status401Unauthorized);

        var token = request.Headers[QueueSessionHeaderName].FirstOrDefault();
        var (layout, error) = await selection.GetLayoutAsync(eventId, userId, token, cancellationToken);
        if (error is not null)
            return Results.Json(
                ApiResponse<object?>.Fail("SELECTION_LAYOUT_FAILED", error),
                statusCode: StatusCodes.Status403Forbidden);

        return Results.Json(ApiResponse<SelectionLayoutResponse>.Ok(layout));
    }

    private static async Task<IResult> HoldAsync(
        Guid eventId,
        HoldSeatRequest body,
        HttpRequest request,
        ClaimsPrincipal user,
        SelectionService selection,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Results.Json(
                ApiResponse<object?>.Fail("AUTH_UNAUTHORIZED", "Unauthorized."),
                statusCode: StatusCodes.Status401Unauthorized);

        var token = request.Headers[QueueSessionHeaderName].FirstOrDefault();
        var (reservation, error) = await selection.HoldAsync(eventId, userId, token, body.SeatId, cancellationToken);
        if (error is not null)
            return Results.Json(
                ApiResponse<object?>.Fail("SELECTION_HOLD_FAILED", error),
                statusCode: StatusCodes.Status400BadRequest);

        return Results.Json(
            ApiResponse<object?>.Ok(new { reservationId = reservation!.Id, expiresAt = reservation!.ExpiresAt }));
    }

    private static async Task<IResult> ReleaseAsync(
        Guid eventId,
        ReleaseSeatRequest body,
        HttpRequest request,
        ClaimsPrincipal user,
        SelectionService selection,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Results.Json(
                ApiResponse<object?>.Fail("AUTH_UNAUTHORIZED", "Unauthorized."),
                statusCode: StatusCodes.Status401Unauthorized);

        var token = request.Headers[QueueSessionHeaderName].FirstOrDefault();
        var (ok, error) = await selection.ReleaseAsync(eventId, userId, token, body.SeatId, cancellationToken);
        if (!ok)
            return Results.Json(
                ApiResponse<object?>.Fail("SELECTION_RELEASE_FAILED", error),
                statusCode: StatusCodes.Status400BadRequest);

        return Results.Json(ApiResponse<object?>.Ok(new { released = true }));
    }
}
