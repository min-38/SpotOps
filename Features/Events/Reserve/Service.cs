using Microsoft.EntityFrameworkCore;
using SpotOps.Data;
using SpotOps.Models;

namespace SpotOps.Features.Events.Reserve;

public sealed class ReserveService
{
    private readonly AppDbContext _db;

    public ReserveService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(Reservation? Reservation, string? Error)> ReserveAsync(
        Guid eventId,
        Guid userId,
        Guid? seatId,
        CancellationToken cancellationToken = default)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (ev == null)
            return (null, "공연을 찾을 수 없어요.");

        // 판매 기간 체크
        var now = DateTime.UtcNow;
        if (now < ev.SaleStartAt || now > ev.SaleEndAt)
            return (null, "판매 기간이 아니에요.");

        // 이미 예약한 경우
        var existing = await _db.Reservations.AnyAsync(r =>
            r.EventId == eventId &&
            r.UserId == userId &&
            (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed),
            cancellationToken);

        if (existing)
            return (null, "이미 예매한 공연이에요.");

        // 좌석 지정형
        if (ev.TicketType == TicketType.Seated)
        {
            if (seatId == null)
                return (null, "좌석을 선택해주세요.");

            var updated = await _db.Seats
                .Where(s => s.Id == seatId && s.EventId == eventId && s.Status == SeatStatus.Available)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, SeatStatus.Reserved), cancellationToken);

            if (updated == 0)
                return (null, "이미 선택된 좌석이에요.");
        }
        else
        {
            // 선착순 — 잔여 인원 체크
            var soldCount = await _db.Reservations.CountAsync(r =>
                r.EventId == eventId &&
                (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed),
                cancellationToken);

            if (soldCount >= ev.TotalCapacity)
                return (null, "매진되었어요.");
        }

        var reservation = new Reservation
        {
            EventId = eventId,
            UserId = userId,
            SeatId = seatId,
            Status = ReservationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync(cancellationToken);

        return (reservation, null);
    }

    public async Task<bool> CancelAsync(Guid reservationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var reservation = await _db.Reservations
            .Include(r => r.Seat)
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId, cancellationToken);

        if (reservation == null || reservation.Status == ReservationStatus.Cancelled)
            return false;

        reservation.Status = ReservationStatus.Cancelled;

        if (reservation.Seat != null)
            reservation.Seat.Status = SeatStatus.Available;

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ReserveResponseDto?> GetStatusAsync(
        Guid eventId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var reservation = await _db.Reservations
            .Where(r => r.EventId == eventId && r.UserId == userId)
            .Where(r => r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed)
            .FirstOrDefaultAsync(cancellationToken);

        if (reservation == null)
            return null;

        return new ReserveResponseDto(reservation.Id, reservation.ExpiresAt);
    }
}
