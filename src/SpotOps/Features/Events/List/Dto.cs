using SpotOps.Features.Events;
using SpotOps.Models;

namespace SpotOps.Features.Events.ListEvents;

public sealed record EventListRowDto(
    Guid Id,
    string Title,
    string VenueName,
    DateTime EventAt,
    decimal Price,
    TicketType TicketType,
    EventSaleStatus SaleStatus);
