using SpotOps.Contracts;

namespace SpotOps.Features.Events.List;

public static class ListEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/events", (ListEventsService list) =>
                Results.Json(ApiResponse<object>.Ok(list.ListActive())))
            .WithName("ListEvents")
            .WithTags("Events")
            .AllowAnonymous();
    }
}
