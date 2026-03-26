using Microsoft.EntityFrameworkCore;
using SpotOps.Data;
using SpotOps.Features.Events;

namespace SpotOps.Features.Events.Detail;

public sealed class EventDetailService
{
    private readonly AppDbContext _db;

    public EventDetailService(AppDbContext db)
    {
        _db = db;
    }

    public EventDetailDto? GetById(Guid id)
    {
        var e = _db.Events
            .Include(x => x.Organizer)
            .FirstOrDefault(x => x.Id == id);

        if (e == null)
            return null;

        return new EventDetailDto(
            e.Id,
            e.Title,
            e.Description,
            e.VenueName,
            e.EventAt,
            e.Price,
            e.TicketType,
            e.SaleStartAt,
            e.SaleEndAt,
            EventSaleStatusResolver.Resolve(e.SaleStartAt, e.SaleEndAt, DateTime.UtcNow));
    }
}
