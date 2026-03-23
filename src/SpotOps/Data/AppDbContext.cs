using Microsoft.EntityFrameworkCore;
using SpotOps.Models;

namespace SpotOps.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Organizer> Organizers => Set<Organizer>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<QueueEntry> QueueEntries => Set<QueueEntry>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // User
        mb.Entity<User>()
            .HasIndex(u => u.Email).IsUnique();
        mb.Entity<User>()
            .Property(u => u.Role).HasConversion<string>();

        // Organizer
        mb.Entity<Organizer>()
            .HasOne(o => o.User)
            .WithOne(u => u.Organizer)
            .HasForeignKey<Organizer>(o => o.UserId);
        mb.Entity<Organizer>()
            .HasIndex(o => o.BusinessNumber).IsUnique();

        // Event
        mb.Entity<Event>()
            .HasOne(e => e.Organizer)
            .WithMany(o => o.Events)
            .HasForeignKey(e => e.OrganizerId);
        mb.Entity<Event>()
            .Property(e => e.TicketType).HasConversion<string>();
        mb.Entity<Event>()
            .Property(e => e.Price).HasColumnType("numeric(10,2)");

        // Seat
        mb.Entity<Seat>()
            .HasOne(s => s.Event)
            .WithMany(e => e.Seats)
            .HasForeignKey(s => s.EventId);
        mb.Entity<Seat>()
            .Property(s => s.RowVersion)
            .IsRowVersion();
        mb.Entity<Seat>()
            .Property(s => s.Status).HasConversion<string>();

        // Reservation
        mb.Entity<Reservation>()
            .HasOne(r => r.User)
            .WithMany(u => u.Reservations)
            .HasForeignKey(r => r.UserId);
        mb.Entity<Reservation>()
            .HasOne(r => r.Event)
            .WithMany(e => e.Reservations)
            .HasForeignKey(r => r.EventId);
        mb.Entity<Reservation>()
            .HasOne(r => r.Seat)
            .WithOne(s => s.Reservation)
            .HasForeignKey<Reservation>(r => r.SeatId);
        mb.Entity<Reservation>()
            .Property(r => r.Status).HasConversion<string>();

        // Ticket
        mb.Entity<Ticket>()
            .HasOne(t => t.Reservation)
            .WithOne(r => r.Ticket)
            .HasForeignKey<Ticket>(t => t.ReservationId);
        mb.Entity<Ticket>()
            .HasIndex(t => t.QrToken).IsUnique();

        // Payment
        mb.Entity<Payment>()
            .HasOne(p => p.Reservation)
            .WithOne(r => r.Payment)
            .HasForeignKey<Payment>(p => p.ReservationId);
        mb.Entity<Payment>()
            .Property(p => p.Amount).HasColumnType("numeric(10,2)");
        mb.Entity<Payment>()
            .Property(p => p.Status).HasConversion<string>();

        // QueueEntry
        mb.Entity<QueueEntry>()
            .HasOne(q => q.Event)
            .WithMany(e => e.QueueEntries)
            .HasForeignKey(q => q.EventId);
        mb.Entity<QueueEntry>()
            .HasOne(q => q.User)
            .WithMany(u => u.QueueEntries)
            .HasForeignKey(q => q.UserId);
        mb.Entity<QueueEntry>()
            .Property(q => q.Status).HasConversion<string>();
        mb.Entity<QueueEntry>()
            .HasIndex(q => new { q.EventId, q.UserId }).IsUnique();
    }
}