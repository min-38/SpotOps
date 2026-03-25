using System.Security.Claims;
using SpotOps.Contracts;

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

    // Task<IResult>: Results.Ok(), Results.BadRequest(), Results.Unauthorized() 등 다양한 HTTP 응답을 반환할 수 있도록 하는 반환 타입
    private static async Task<IResult> JoinAsync(
        Guid eventId,
        QueueService queue,
        ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Results.Json(
                ApiResponse<object?>.Fail("AUTH_UNAUTHORIZED", "Unauthorized."),
                statusCode: StatusCodes.Status401Unauthorized);

        var res = await queue.JoinAsync(eventId, userId);
        return Results.Json(ApiResponse<QueueJoinResponse>.Ok(res));
    }

    private static async Task<IResult> GetStatusAsync(
        Guid eventId,
        Guid queueEntryId,
        QueueService queue)
    {
        try
        {
            var res = await queue.GetStatusAsync(eventId, queueEntryId);
            return Results.Json(ApiResponse<QueueStatusResponse>.Ok(res));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(
                ApiResponse<object?>.Fail("QUEUE_NOT_FOUND", ex.Message),
                statusCode: StatusCodes.Status404NotFound);
        }
    }

    private sealed record InviteNextRequest(int BatchSize, int SelectionWindowSec);

    private static async Task<IResult> InviteNextBatchAsync(
        Guid eventId,
        InviteNextRequest body,
        QueueService queue)
    {
        if (body.BatchSize <= 0 || body.SelectionWindowSec <= 0)
            return Results.Json(
                ApiResponse<object?>.Fail(
                    "INVITE_NEXT_INVALID_REQUEST",
                    "BatchSize and SelectionWindowSec must be positive."),
                statusCode: StatusCodes.Status400BadRequest);

        var invited = await queue.InviteNextBatchAsync(eventId, body.BatchSize, body.SelectionWindowSec);
        return Results.Json(ApiResponse<int>.Ok(invited));
    }
}
