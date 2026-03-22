namespace SpotOps.Features.Events.Detail;

public static class DetailEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/events/{id:guid}", (Guid id, EventDetailService details) =>
            {
                var dto = details.GetById(id);
                return dto is null ? Results.NotFound() : Results.Json(dto);
            })
            .WithName("GetEvent")
            .WithTags("Events")
            .AllowAnonymous();
    }
}
