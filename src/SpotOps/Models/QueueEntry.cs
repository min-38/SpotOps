namespace SpotOps.Models;

public class QueueEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public int Position { get; set; }
    public QueueStatus Status { get; set; } = QueueStatus.Waiting;
    public DateTime EnteredAt { get; set; } = DateTime.UtcNow;
    
    public Event Event { get; set; } = null!;
    public User User { get; set; } = null!;
}

public enum QueueStatus { Waiting, Admitted, Expired }