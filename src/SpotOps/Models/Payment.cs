namespace SpotOps.Models;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReservationId { get; set; }

    // 포트원 결제 모듈 V2에 넘기는 paymentId(고객사 주문번호).
    public string PortOnePaymentId { get; set; } = "";

    public decimal Amount { get; set; }
    public string? PgTransactionId { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime? PaidAt { get; set; }

    public Reservation Reservation { get; set; } = null!;
}

public enum PaymentStatus { Pending, Paid, Refunded }