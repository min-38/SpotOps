using SpotOps.Models;

namespace SpotOps.Features.Me.Reservations;

public sealed record MyReservationEventDto(
    Guid Id,
    string Title,
    DateTime EventAt,
    string VenueName,
    decimal Price,
    TicketType TicketType);

public sealed record MyReservationSeatDto(
    Guid Id,
    string Section,
    string Row,
    string Number,
    SeatStatus Status);

public sealed record MyReservationPaymentDto(
    string PortOnePaymentId,
    decimal Amount,
    PaymentStatus Status,
    DateTime? PaidAt);

public sealed record MyReservationTicketDto(
    Guid Id,
    bool IsUsed,
    DateTime? UsedAt,
    DateTime IssuedAt);

public sealed record MyReservationDto(
    Guid ReservationId,
    ReservationStatus Status,
    DateTime ExpiresAt,
    DateTime CreatedAt,
    MyReservationEventDto Event,
    MyReservationSeatDto? Seat,
    MyReservationPaymentDto? Payment,
    MyReservationTicketDto? Ticket);

public sealed record CancelReservationResultDto(
    Guid ReservationId,
    ReservationStatus Status,
    decimal RefundRate,
    decimal RefundAmount,
    string PolicyReason);
