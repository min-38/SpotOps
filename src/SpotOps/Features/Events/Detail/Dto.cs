using SpotOps.Features.Events;
using SpotOps.Models;

namespace SpotOps.Features.Events.Detail;

public sealed record EventDetailDto(
    Guid Id,
    string Title,
    string? Description,
    string VenueName,
    DateTime EventAt,
    decimal Price,
    TicketType TicketType,
    DateTime SaleStartAt,
    DateTime SaleEndAt,
    EventSaleStatus SaleStatus);
