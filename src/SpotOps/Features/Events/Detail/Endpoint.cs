using SpotOps.Contracts;

namespace SpotOps.Features.Events.Detail;

public static class DetailEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/events/{id:guid}", (Guid id, EventDetailService details) =>
            {
                var dto = details.GetById(id);
            return dto is null
                ? Results.Json(ApiResponse<object?>.Fail("EVENT_NOT_FOUND"), statusCode: StatusCodes.Status404NotFound)
                : Results.Json(ApiResponse<EventDetailDto>.Ok(dto));
            })
            .WithName("GetEvent")
            .WithTags("Events")
            .AllowAnonymous();
    }
}
