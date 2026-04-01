namespace SpotOps.Models;

public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>이벤트를 등록한 운영 계정(일반 유저). 업체 테이블 도입 전까지 사용.</summary>
    public Guid? CreatedByUserId { get; set; }
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
    
    public ICollection<Seat> Seats { get; set; } = [];
    public ICollection<Reservation> Reservations { get; set; } = [];
    public ICollection<QueueEntry> QueueEntries { get; set; } = [];
}

public enum TicketType { Seated, Standing }