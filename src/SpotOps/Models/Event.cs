namespace SpotOps.Models;

public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizerId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public TicketType TicketType { get; set; }
    public DateTime EventAt { get; set; }
    public DateTime SaleStartAt { get; set; }
    public DateTime SaleEndAt { get; set; }
    public int TotalCapacity { get; set; }
    public decimal Price { get; set; }
    public string VenueName { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Organizer Organizer { get; set; } = null!;
    public ICollection<Seat> Seats { get; set; } = [];
    public ICollection<Reservation> Reservations { get; set; } = [];
    public ICollection<QueueEntry> QueueEntries { get; set; } = [];
}

public enum TicketType { Seated, Standing }