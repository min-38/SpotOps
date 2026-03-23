using System.Security.Claims;

namespace SpotOps.Features.Events.Queue;

public static class QueueEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/events/{eventId:guid}/queue")
            .WithTags("Events.Queue")
            .RequireAuthorization();

        group.MapPost("/join", JoinAsync);
        group.MapGet("/status/{queueEntryId:guid}", GetStatusAsync);

        // 임시 운영용 (나중에 워커/스케줄러로 이동)
        group.MapPost("/invite-next", InviteNextBatchAsync);
    }

    private static IResult JoinAsync(
        Guid eventId,
        QueueService queue,
        ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Results.Unauthorized();

        var res = queue.Join(eventId, userId);
        return Results.Json(res);
    }

    private static IResult GetStatusAsync(
        Guid eventId,
        Guid queueEntryId,
        QueueService queue)
    {
        try
        {
            var res = queue.GetStatus(eventId, queueEntryId);
            return Results.Json(res);
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }

    private sealed record InviteNextRequest(int BatchSize, int SelectionWindowSec);

    private static IResult InviteNextBatchAsync(
        Guid eventId,
        InviteNextRequest body,
        QueueService queue)
    {
        if (body.BatchSize <= 0 || body.SelectionWindowSec <= 0)
            return Results.BadRequest(new { error = "BatchSize and SelectionWindowSec must be positive." });

        var invited = queue.InviteNextBatch(eventId, body.BatchSize, body.SelectionWindowSec);
        return Results.Json(new { invited });
    }
}
