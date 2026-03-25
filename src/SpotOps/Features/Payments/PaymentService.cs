using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SpotOps.Data;
using SpotOps.Infrastructure.PortOne;
using SpotOps.Models;

namespace SpotOps.Features.Payments;

public sealed class PaymentService
{
    private readonly AppDbContext _db;
    private readonly IPortOnePaymentApi _portOne;
    private readonly PortOneOptions _portOneOpt;

    public PaymentService(
        AppDbContext db,
        IPortOnePaymentApi portOne,
        IOptions<PortOneOptions> portOneOptions)
    {
        _db = db;
        _portOne = portOne;
        _portOneOpt = portOneOptions.Value;
    }

    public async Task<(PaymentPrepareResponse? Response, string? Error)> PrepareAsync(
        Guid userId,
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        var reservation = await _db.Reservations
            .Include(r => r.Event)
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId, cancellationToken);

        if (reservation == null)
            return (null, "예약을 찾을 수 없어요.");
        if (reservation.Status != ReservationStatus.Pending)
            return (null, "결제할 수 있는 예약 상태가 아니에요.");
        if (DateTime.UtcNow > reservation.ExpiresAt)
            return (null, "예약이 만료되었어요.");

        var ev = reservation.Event;
        var amount = (long)Math.Round(ev.Price, 0, MidpointRounding.AwayFromZero);
        if (amount <= 0)
            return (null, "결제 금액이 올바르지 않아요.");

        if (reservation.Payment != null)
        {
            var p = reservation.Payment;
            if (p.Status == PaymentStatus.Paid)
                return (null, "이미 결제가 완료된 예약이에요.");

            return (new PaymentPrepareResponse(
                p.PortOnePaymentId,
                _portOneOpt.StoreId,
                amount,
                ev.Title), null);
        }

        var paymentId = $"spotops-{reservationId:N}";
        var payment = new Payment
        {
            ReservationId = reservation.Id,
            PortOnePaymentId = paymentId,
            Amount = ev.Price,
            Status = PaymentStatus.Pending
        };
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(cancellationToken);

        return (new PaymentPrepareResponse(
            paymentId,
            _portOneOpt.StoreId,
            amount,
            ev.Title), null);
    }

    /// <summary>
    /// 웹훅 본문 처리. <c>Transaction.Paid</c>만 반영. 알 수 없는 type은 무시(문서 권장).
    /// </summary>
    public async Task ProcessPortOneWebhookAsync(string rawJson, CancellationToken cancellationToken = default)
    {
        if (!PortOneWebhookParser.TryGetTransactionPaid(rawJson, out var paymentId, out var transactionId, out var storeId))
            return;

        if (!string.IsNullOrEmpty(_portOneOpt.StoreId)
            && !string.IsNullOrEmpty(storeId)
            && storeId != _portOneOpt.StoreId)
            return;

        // InMemory 공급자는 트랜잭션을 지원하지 않음(단위 테스트).
        if (UsesInMemoryProvider(_db))
        {
            await ProcessPortOneWebhookCoreAsync(paymentId!, transactionId, cancellationToken);
            return;
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        await ProcessPortOneWebhookCoreAsync(paymentId!, transactionId, cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    private async Task ProcessPortOneWebhookCoreAsync(
        string paymentId,
        string? transactionId,
        CancellationToken cancellationToken)
    {
        var payment = await _db.Payments
            .Include(p => p.Reservation)
            .ThenInclude(r => r.Event)
            .Include(p => p.Reservation)
            .ThenInclude(r => r.Seat)
            .Include(p => p.Reservation)
            .ThenInclude(r => r.Ticket)
            .FirstOrDefaultAsync(p => p.PortOnePaymentId == paymentId, cancellationToken);

        if (payment == null || payment.Status == PaymentStatus.Paid)
            return;

        var remote = await _portOne.GetPaymentAsync(paymentId, cancellationToken);
        if (remote == null)
            return;

        var payRoot = PortOnePaymentResponseParser.GetPaymentRoot(remote);
        if (!PortOnePaymentResponseParser.IsPaid(payRoot)
            || !PortOnePaymentResponseParser.TryGetTotalAmount(payRoot, out var remoteTotal))
            return;

        var expected = (long)Math.Round(payment.Amount, 0, MidpointRounding.AwayFromZero);
        if (remoteTotal != expected)
            return;

        var reservation = payment.Reservation;
        if (reservation.Status != ReservationStatus.Pending)
            return;

        payment.Status = PaymentStatus.Paid;
        payment.PaidAt = DateTime.UtcNow;
        payment.PgTransactionId = transactionId;

        reservation.Status = ReservationStatus.Confirmed;

        if (reservation.Seat != null)
            reservation.Seat.Status = SeatStatus.Sold;

        if (reservation.Ticket == null)
        {
            _db.Tickets.Add(new Ticket
            {
                ReservationId = reservation.Id
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static bool UsesInMemoryProvider(AppDbContext db) =>
        db.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true;
}
