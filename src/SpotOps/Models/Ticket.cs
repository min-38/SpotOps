namespace SpotOps.Models;

public class Ticket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReservationId { get; set; }
    public string QrToken { get; set; } = Guid.NewGuid().ToString();
    public bool IsUsed { get; set; } = false;
    public DateTime? UsedAt { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    
    public Reservation Reservation { get; set; } = null!;
}