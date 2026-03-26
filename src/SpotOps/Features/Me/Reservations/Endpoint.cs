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
}

