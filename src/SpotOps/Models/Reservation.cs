namespace SpotOps.Models;

public class Reservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public Guid? SeatId { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(10);
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Event Event { get; set; } = null!;
    public User User { get; set; } = null!;
    public Seat? Seat { get; set; }
    public Ticket? Ticket { get; set; }
    public Payment? Payment { get; set; }
}

public enum ReservationStatus { Pending, Confirmed, Cancelled }