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
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // User
        mb.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.Property(u => u.Id).HasColumnName("id");
            entity.Property(u => u.Email).HasColumnName("email");
            entity.Property(u => u.PasswordHash).HasColumnName("password_hash");
            entity.Property(u => u.Name).HasColumnName("name");
            entity.Property(u => u.Phone).HasColumnName("phone");
            entity.Property(u => u.PhoneVerifiedAt).HasColumnName("phone_verified_at");
            entity.Property(u => u.Role).HasColumnName("role").HasConversion<string>();
            entity.Property(u => u.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(u => u.Email).IsUnique().HasDatabaseName("ix_users_email");
        });

        // Organizer
        mb.Entity<Organizer>(entity =>
        {
            entity.ToTable("organizers");
            entity.Property(o => o.Id).HasColumnName("id");
            entity.Property(o => o.UserId).HasColumnName("user_id");
            entity.Property(o => o.BusinessNumber).HasColumnName("business_number");
            entity.Property(o => o.CompanyName).HasColumnName("company_name");
            entity.Property(o => o.IsVerified).HasColumnName("is_verified");
            entity.Property(o => o.CreatedAt).HasColumnName("created_at");

            entity.HasOne(o => o.User)
                .WithOne(u => u.Organizer)
                .HasForeignKey<Organizer>(o => o.UserId)
                .HasConstraintName("fk_organizers_users_user_id");
            entity.HasIndex(o => o.BusinessNumber)
                .IsUnique()
                .HasDatabaseName("ix_organizers_business_number");
            entity.HasIndex(o => o.UserId)
                .IsUnique()
                .HasDatabaseName("ix_organizers_user_id");
        });

        // Event
        mb.Entity<Event>(entity =>
        {
            entity.ToTable("events");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizerId).HasColumnName("organizer_id");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.TicketType).HasColumnName("ticket_type").HasConversion<string>();
            entity.Property(e => e.EventAt).HasColumnName("event_at");
            entity.Property(e => e.SaleStartAt).HasColumnName("sale_start_at");
            entity.Property(e => e.SaleEndAt).HasColumnName("sale_end_at");
            entity.Property(e => e.TotalCapacity).HasColumnName("total_capacity");
            entity.Property(e => e.Price).HasColumnName("price").HasColumnType("numeric(10,2)");
            entity.Property(e => e.VenueName).HasColumnName("venue_name");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.Organizer)
                .WithMany(o => o.Events)
                .HasForeignKey(e => e.OrganizerId)
                .HasConstraintName("fk_events_organizers_organizer_id");
            entity.HasIndex(e => e.OrganizerId).HasDatabaseName("ix_events_organizer_id");
        });

        // Seat
        mb.Entity<Seat>(entity =>
        {
            entity.ToTable("seats");
            entity.Property(s => s.Id).HasColumnName("id");
            entity.Property(s => s.EventId).HasColumnName("event_id");
            entity.Property(s => s.Section).HasColumnName("section");
            entity.Property(s => s.Row).HasColumnName("row");
            entity.Property(s => s.RowVersion).HasColumnName("row_version").IsRowVersion();
            entity.Property(s => s.Number).HasColumnName("number");
            entity.Property(s => s.Status).HasColumnName("status").HasConversion<string>();

            entity.HasOne(s => s.Event)
                .WithMany(e => e.Seats)
                .HasForeignKey(s => s.EventId)
                .HasConstraintName("fk_seats_events_event_id");
            entity.HasIndex(s => s.EventId).HasDatabaseName("ix_seats_event_id");
        });

        // Reservation
        mb.Entity<Reservation>(entity =>
        {
            entity.ToTable("reservations");
            entity.Property(r => r.Id).HasColumnName("id");
            entity.Property(r => r.EventId).HasColumnName("event_id");
            entity.Property(r => r.UserId).HasColumnName("user_id");
            entity.Property(r => r.SeatId).HasColumnName("seat_id");
            entity.Property(r => r.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(r => r.ExpiresAt).HasColumnName("expires_at");
            entity.Property(r => r.CreatedAt).HasColumnName("created_at");

            entity.HasOne(r => r.User)
                .WithMany(u => u.Reservations)
                .HasForeignKey(r => r.UserId)
                .HasConstraintName("fk_reservations_users_user_id");
            entity.HasOne(r => r.Event)
                .WithMany(e => e.Reservations)
                .HasForeignKey(r => r.EventId)
                .HasConstraintName("fk_reservations_events_event_id");
            entity.HasOne(r => r.Seat)
                .WithOne(s => s.Reservation)
                .HasForeignKey<Reservation>(r => r.SeatId)
                .HasConstraintName("fk_reservations_seats_seat_id");
            entity.HasIndex(r => r.UserId).HasDatabaseName("ix_reservations_user_id");
            entity.HasIndex(r => r.EventId).HasDatabaseName("ix_reservations_event_id");
            entity.HasIndex(r => r.SeatId).HasDatabaseName("ix_reservations_seat_id");
        });

        // Ticket
        mb.Entity<Ticket>(entity =>
        {
            entity.ToTable("tickets");
            entity.Property(t => t.Id).HasColumnName("id");
            entity.Property(t => t.ReservationId).HasColumnName("reservation_id");
            entity.Property(t => t.QrToken).HasColumnName("qr_token");
            entity.Property(t => t.IsUsed).HasColumnName("is_used");
            entity.Property(t => t.UsedAt).HasColumnName("used_at");
            entity.Property(t => t.IssuedAt).HasColumnName("issued_at");

            entity.HasOne(t => t.Reservation)
                .WithOne(r => r.Ticket)
                .HasForeignKey<Ticket>(t => t.ReservationId)
                .HasConstraintName("fk_tickets_reservations_reservation_id");
            entity.HasIndex(t => t.ReservationId).HasDatabaseName("ix_tickets_reservation_id");
            entity.HasIndex(t => t.QrToken).IsUnique().HasDatabaseName("ix_tickets_qr_token");
        });

        // Payment
        mb.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");
            entity.Property(p => p.Id).HasColumnName("id");
            entity.Property(p => p.ReservationId).HasColumnName("reservation_id");
            entity.Property(p => p.PortOnePaymentId).HasColumnName("port_one_payment_id");
            entity.Property(p => p.Amount).HasColumnName("amount").HasColumnType("numeric(10,2)");
            entity.Property(p => p.PgTransactionId).HasColumnName("pg_transaction_id");
            entity.Property(p => p.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(p => p.PaidAt).HasColumnName("paid_at");

            entity.HasOne(p => p.Reservation)
                .WithOne(r => r.Payment)
                .HasForeignKey<Payment>(p => p.ReservationId)
                .HasConstraintName("fk_payments_reservations_reservation_id");
            entity.HasIndex(p => p.ReservationId).HasDatabaseName("ix_payments_reservation_id");
            entity.HasIndex(p => p.PortOnePaymentId).IsUnique().HasDatabaseName("ix_payments_port_one_payment_id");
        });

        // QueueEntry
        mb.Entity<QueueEntry>(entity =>
        {
            entity.ToTable("queue_entries");
            entity.Property(q => q.Id).HasColumnName("id");
            entity.Property(q => q.EventId).HasColumnName("event_id");
            entity.Property(q => q.UserId).HasColumnName("user_id");
            entity.Property(q => q.Position).HasColumnName("position");
            entity.Property(q => q.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(q => q.EnteredAt).HasColumnName("entered_at");

            entity.HasOne(q => q.Event)
                .WithMany(e => e.QueueEntries)
                .HasForeignKey(q => q.EventId)
                .HasConstraintName("fk_queue_entries_events_event_id");
            entity.HasOne(q => q.User)
                .WithMany(u => u.QueueEntries)
                .HasForeignKey(q => q.UserId)
                .HasConstraintName("fk_queue_entries_users_user_id");
            entity.HasIndex(q => q.UserId).HasDatabaseName("ix_queue_entries_user_id");
            entity.HasIndex(q => new { q.EventId, q.UserId })
                .IsUnique()
                .HasDatabaseName("ix_queue_entries_event_id_user_id");
        });

        // PasswordResetToken
        mb.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("password_reset_tokens");
            entity.Property(t => t.Id).HasColumnName("id");
            entity.Property(t => t.UserId).HasColumnName("user_id");
            entity.Property(t => t.TokenHash).HasColumnName("token_hash");
            entity.Property(t => t.ExpiresAt).HasColumnName("expires_at");
            entity.Property(t => t.CreatedAt).HasColumnName("created_at");
            entity.Property(t => t.UsedAt).HasColumnName("used_at");

            entity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .HasConstraintName("fk_password_reset_tokens_users_user_id");
            entity.HasIndex(t => t.UserId).HasDatabaseName("ix_password_reset_tokens_user_id");
            entity.HasIndex(t => t.TokenHash).IsUnique().HasDatabaseName("ix_password_reset_tokens_token_hash");
        });

        // RefreshToken
        mb.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.Property(t => t.Id).HasColumnName("id");
            entity.Property(t => t.UserId).HasColumnName("user_id");
            entity.Property(t => t.TokenHash).HasColumnName("token_hash");
            entity.Property(t => t.ExpiresAt).HasColumnName("expires_at");
            entity.Property(t => t.CreatedAt).HasColumnName("created_at");
            entity.Property(t => t.RevokedAt).HasColumnName("revoked_at");

            entity.HasOne(t => t.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(t => t.UserId)
                .HasConstraintName("fk_refresh_tokens_users_user_id");
            entity.HasIndex(t => t.TokenHash)
                .IsUnique()
                .HasDatabaseName("ix_refresh_tokens_token_hash");
            entity.HasIndex(t => t.UserId)
                .HasDatabaseName("ix_refresh_tokens_user_id");
        });
    }
}