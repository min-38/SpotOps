using System.Security.Claims;
using SpotOps.Contracts;
using SpotOps.Data;
using Microsoft.EntityFrameworkCore;

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
        AppDbContext db,
        QueueService queue,
        ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Results.Json(
                ApiResponse<object?>.Fail("AUTH_UNAUTHORIZED", "Unauthorized."),
                statusCode: StatusCodes.Status401Unauthorized);

        var isPhoneVerified = await db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId && u.PhoneVerifiedAt != null);
        if (!isPhoneVerified)
            return Results.Json(
                ApiResponse<object?>.Fail("PHONE_VERIFICATION_REQUIRED", "휴대폰 인증 후 순서권을 발급할 수 있어요."),
                statusCode: StatusCodes.Status403Forbidden);

        try
        {
            var res = await queue.JoinAsync(eventId, userId);
            return Results.Json(ApiResponse<QueueJoinResponse>.Ok(res));
        }
        catch (QueueException.QueueBusyException ex)
        {
            return Results.Json(
                ApiResponse<object?>.Fail("QUEUE_BUSY", ex.Message),
                statusCode: StatusCodes.Status409Conflict);
        }
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
        catch (QueueException.QueueBusyException ex)
        {
            return Results.Json(
                ApiResponse<object?>.Fail("QUEUE_BUSY", ex.Message),
                statusCode: StatusCodes.Status409Conflict);
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

        try
        {
            var invited = await queue.InviteNextBatchAsync(eventId, body.BatchSize, body.SelectionWindowSec);
            return Results.Json(ApiResponse<int>.Ok(invited));
        }
        catch (QueueException.QueueBusyException ex)
        {
            return Results.Json(
                ApiResponse<object?>.Fail("QUEUE_BUSY", ex.Message),
                statusCode: StatusCodes.Status409Conflict);
        }
    }
}
