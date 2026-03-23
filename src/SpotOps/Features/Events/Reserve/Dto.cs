namespace SpotOps.Features.Events.Reserve;

public record ReserveRequestDto(Guid? SeatId);

public record ReserveResponseDto(
    Guid ReservationId,
    DateTime ExpiresAt
);
