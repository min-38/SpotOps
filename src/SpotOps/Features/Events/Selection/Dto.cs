namespace SpotOps.Features.Events.Selection;

public sealed record SeatLayoutItemDto(
    Guid Id,
    string Section,
    string Row,
    string Number,
    string Status);

public sealed record SelectionLayoutResponse(bool IsSeated, IReadOnlyList<SeatLayoutItemDto> Seats);

public sealed record HoldSeatRequest(Guid SeatId);

public sealed record ReleaseSeatRequest(Guid SeatId);
