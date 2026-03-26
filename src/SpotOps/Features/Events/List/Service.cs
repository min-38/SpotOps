using SpotOps.Data;
using SpotOps.Features.Events;
using SpotOps.Features.Events.ListEvents;

namespace SpotOps.Features.Events.List;

public sealed class ListEventsService
{
    private readonly AppDbContext _db;

    public ListEventsService(AppDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<EventListRowDto> ListActive()
    {
        var now = DateTime.UtcNow;
        return _db.Events
            .OrderBy(e => e.EventAt)
            .Select(e => new EventListRowDto(
                e.Id,
                e.Title,
                e.VenueName,
                e.EventAt,
                e.Price,
                e.TicketType,
                EventSaleStatusResolver.Resolve(e.SaleStartAt, e.SaleEndAt, now)))
            .ToList();
    }
}
