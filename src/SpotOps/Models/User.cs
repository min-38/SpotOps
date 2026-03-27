using System.ComponentModel.DataAnnotations;

namespace SpotOps.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string Email { get; set; } = "";
    
    [Required]
    public string PasswordHash { get; set; } = "";
    
    [Required]
    public string Name { get; set; } = "";

    public string? Phone { get; set; }
    public DateTime? PhoneVerifiedAt { get; set; }

    public UserRole Role { get; set; } = UserRole.Buyer;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Organizer? Organizer { get; set; }
    public ICollection<Reservation> Reservations { get; set; } = [];
    public ICollection<QueueEntry> QueueEntries { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}

public enum UserRole { Buyer, Organizer, Admin }