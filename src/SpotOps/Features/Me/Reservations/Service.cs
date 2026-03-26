using Microsoft.EntityFrameworkCore;
using SpotOps.Data;
using SpotOps.Models;

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

    public async Task<(CancelReservationResultDto? Result, string? ErrorCode, string? ErrorMessage)> CancelAsync(
        Guid userId,
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        var reservation = await _db.Reservations
            .Include(r => r.Event)
            .Include(r => r.Seat)
            .Include(r => r.Payment)
            .Include(r => r.Ticket)
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId, cancellationToken);

        if (reservation is null)
            return (null, "ME_RESERVATION_NOT_FOUND", "예매 내역을 찾을 수 없어요.");

        if (reservation.Status == ReservationStatus.Cancelled)
            return (null, "ME_RESERVATION_ALREADY_CANCELLED", "이미 취소된 예매예요.");

        if (reservation.Ticket?.IsUsed == true)
            return (null, "ME_RESERVATION_TICKET_ALREADY_USED", "이미 사용된 티켓은 취소할 수 없어요.");

        var (refundRate, policyReason) = GetRefundPolicy(reservation.Event.EventAt, DateTime.UtcNow);
        var refundAmount = 0m;

        if (reservation.Payment is not null && reservation.Payment.Status == PaymentStatus.Paid)
        {
            refundAmount = decimal.Round(reservation.Payment.Amount * refundRate, 2, MidpointRounding.AwayFromZero);
            reservation.Payment.Status = PaymentStatus.Refunded;
        }

        reservation.Status = ReservationStatus.Cancelled;
        if (reservation.Seat is not null)
            reservation.Seat.Status = SeatStatus.Available;

        await _db.SaveChangesAsync(cancellationToken);

        return (new CancelReservationResultDto(
                reservation.Id,
                reservation.Status,
                refundRate,
                refundAmount,
                policyReason),
            null,
            null);
    }

    private static (decimal RefundRate, string PolicyReason) GetRefundPolicy(DateTime eventAtUtc, DateTime nowUtc)
    {
        var diff = eventAtUtc - nowUtc;
        if (diff >= TimeSpan.FromDays(7))
            return (1.0m, "공연 7일 전 취소: 100% 환불");

        if (diff >= TimeSpan.FromDays(3))
            return (0.5m, "공연 3일 전 취소: 50% 환불");

        return (0.0m, "공연 3일 미만 취소: 환불 불가");
    }
}

