namespace SpotOps.Models;

public class Seat
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public string Section { get; set; } = "";
    public string Row { get; set; } = "";
    public byte[] RowVersion { get; set; } = [];
    public string Number { get; set; } = "";
    public SeatStatus Status { get; set; } = SeatStatus.Available;
    
    public Event Event { get; set; } = null!;
    public Reservation? Reservation { get; set; }
}

public enum SeatStatus { Available, Reserved, Sold }