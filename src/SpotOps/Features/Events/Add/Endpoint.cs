using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SpotOps.Data;
using SpotOps.Models;
using SpotOps.Contracts;
using System.Security.Claims;

namespace SpotOps.Features.Events.Add;

public static class AddEventEndpoint
{
    public static void Map(WebApplication app)
    {
        void OrganizerOnly(AuthorizationPolicyBuilder p) => p.RequireRole(nameof(UserRole.Organizer));

        app.MapPost("/api/events", CreateAsync)
            .WithTags("Events")
            .RequireAuthorization(OrganizerOnly);

        app.MapPost("/api/organizer/events", CreateAsync)
            .WithTags("Events", "Organizer")
            .RequireAuthorization(OrganizerOnly);
    }

    private static async Task<IResult> CreateAsync(
        AddEventDto body,
        AddEventService addEvents,
        AppDbContext db,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return Results.Json(
                ApiResponse<object?>.Fail("AUTH_UNAUTHORIZED", "Unauthorized."),
                statusCode: StatusCodes.Status401Unauthorized);

        var organizer = await db.Organizers.FirstOrDefaultAsync(o => o.UserId == userId, cancellationToken);
        if (organizer is null)
            return Results.Json(
                ApiResponse<object?>.Fail("AUTH_ORGANIZER_PROFILE_MISSING", "주최자 프로필이 없습니다."),
                statusCode: StatusCodes.Status403Forbidden);

        var ev = await addEvents.AddAsync(organizer.Id, body, cancellationToken);
        return Results.Json(ApiResponse<Guid>.Ok(ev.Id), statusCode: StatusCodes.Status201Created);
    }
}
