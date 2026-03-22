using SpotOps.Models;

namespace SpotOps.Features.Events.Add;

public class AddEventDto
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public TicketType TicketType { get; set; }
    public DateTime EventAt { get; set; }
    public DateTime SaleStartAt { get; set; }
    public DateTime SaleEndAt { get; set; }
    public int TotalCapacity { get; set; }
    public decimal Price { get; set; }
    public string VenueName { get; set; } = "";
}
