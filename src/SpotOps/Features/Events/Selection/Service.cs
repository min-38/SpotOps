using Microsoft.EntityFrameworkCore;
using SpotOps.Data;
using SpotOps.Features.Events.Queue;
using SpotOps.Features.Events.Reserve;
using SpotOps.Models;

namespace SpotOps.Features.Events.Selection;

public sealed class SelectionService
{
    private readonly AppDbContext _db;
    private readonly QueueService _queue;
    private readonly ReserveService _reserve;

    public SelectionService(AppDbContext db, QueueService queue, ReserveService reserve)
    {
        _db = db;
        _queue = queue;
        _reserve = reserve;
    }

    // 좌석 레이아웃 조회
    public async Task<(SelectionLayoutResponse? Layout, string? Error)> GetLayoutAsync(
        Guid eventId,
        Guid userId,
        string? sessionToken,
        CancellationToken cancellationToken = default)
    {
        if (!await _queue.ValidateSelectionSessionAsync(eventId, userId, sessionToken, cancellationToken))
            return (null, "유효한 대기열 세션이 아니에요.");

        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);
        if (ev == null)
            return (null, "공연을 찾을 수 없어요.");

        if (ev.TicketType != TicketType.Seated)
            return (new SelectionLayoutResponse(IsSeated: false, Seats: []), null);

        var raw = await _db.Seats.AsNoTracking()
            .Where(s => s.EventId == eventId)
            .OrderBy(s => s.Section).ThenBy(s => s.Row).ThenBy(s => s.Number)
            .ToListAsync(cancellationToken);

        var seats = raw
            .Select(s => new SeatLayoutItemDto(s.Id, s.Section, s.Row, s.Number, s.Status.ToString()))
            .ToList();

        return (new SelectionLayoutResponse(IsSeated: true, Seats: seats), null);
    }
    
    // 한 유저가 좌석을 결제 중이면, 다른 유저가 해당 좌석을 예약할 수 없도록 막는다.
    public async Task<(Reservation? Reservation, string? Error)> HoldAsync(
        Guid eventId,
        Guid userId,
        string? sessionToken,
        Guid seatId,
        CancellationToken cancellationToken = default)
    {
        if (!await _queue.ValidateSelectionSessionAsync(eventId, userId, sessionToken, cancellationToken))
            return (null, "유효한 대기열 세션이 아니에요.");

        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);
        if (ev == null)
            return (null, "공연을 찾을 수 없어요.");

        if (ev.TicketType != TicketType.Seated)
            return (null, "좌석 지정 공연이 아니에요.");

        return await _reserve.ReserveAsync(eventId, userId, seatId, cancellationToken);
    }

    // 결제 성공했거나, 좌석 예약 만료되었으면, 좌석 예약을 취소해 다른 유저가 예약할 수 있도록 한다.
    public async Task<(bool Ok, string? Error)> ReleaseAsync(
        Guid eventId,
        Guid userId,
        string? sessionToken,
        Guid seatId,
        CancellationToken cancellationToken = default)
    {
        if (!await _queue.ValidateSelectionSessionAsync(eventId, userId, sessionToken, cancellationToken))
            return (false, "유효한 대기열 세션이 아니에요.");

        var reservation = await _db.Reservations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.EventId == eventId
                     && r.UserId == userId
                     && r.SeatId == seatId
                     && r.Status == ReservationStatus.Pending,
                cancellationToken);

        if (reservation == null)
            return (false, "취소할 예약을 찾을 수 없어요.");

        var cancelled = await _reserve.CancelAsync(reservation.Id, userId, cancellationToken);
        return cancelled ? (true, null) : (false, "예약 취소에 실패했어요.");
    }
}
