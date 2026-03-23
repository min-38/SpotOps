using SpotOps.Data;
using SpotOps.Models;

namespace SpotOps.Features.Events.Add;

public sealed class AddEventService
{
    private readonly AppDbContext _db;

    public AddEventService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Event> AddAsync(Guid organizerId, AddEventDto dto, CancellationToken cancellationToken = default)
    {
        var ev = new Event
        {
            OrganizerId = organizerId,
            Title = dto.Title,
            Description = dto.Description,
            TicketType = dto.TicketType,
            EventAt = dto.EventAt.ToUniversalTime(),
            SaleStartAt = dto.SaleStartAt.ToUniversalTime(),
            SaleEndAt = dto.SaleEndAt.ToUniversalTime(),
            TotalCapacity = dto.TotalCapacity,
            Price = dto.Price,
            VenueName = dto.VenueName
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync(cancellationToken);
        return ev;
    }
}
