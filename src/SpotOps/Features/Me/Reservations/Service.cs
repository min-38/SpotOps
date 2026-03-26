using Microsoft.EntityFrameworkCore;
using SpotOps.Data;

namespace SpotOps.Features.Me.Reservations;

public sealed class MyReservationsService
{
    private readonly AppDbContext _db;

    public MyReservationsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<MyReservationDto>> ListAsync(Guid userId, int take = 50, CancellationToken cancellationToken = default)
    {
        if (take is <= 0 or > 200) take = 50;

        return await _db.Reservations
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(take)
            .Select(r => new MyReservationDto(
                ReservationId: r.Id,
                Status: r.Status,
                ExpiresAt: r.ExpiresAt,
                CreatedAt: r.CreatedAt,
                Event: new MyReservationEventDto(
                    r.Event.Id,
                    r.Event.Title,
                    r.Event.EventAt,
                    r.Event.VenueName,
                    r.Event.Price,
                    r.Event.TicketType),
                Seat: r.SeatId == null
                    ? null
                    : new MyReservationSeatDto(
                        r.Seat!.Id,
                        r.Seat.Section,
                        r.Seat.Row,
                        r.Seat.Number,
                        r.Seat.Status),
                Payment: r.Payment == null
                    ? null
                    : new MyReservationPaymentDto(
                        r.Payment.PortOnePaymentId,
                        r.Payment.Amount,
                        r.Payment.Status,
                        r.Payment.PaidAt),
                Ticket: r.Ticket == null
                    ? null
                    : new MyReservationTicketDto(
                        r.Ticket.Id,
                        r.Ticket.IsUsed,
                        r.Ticket.UsedAt,
                        r.Ticket.IssuedAt)))
            .ToListAsync(cancellationToken);
    }
}

