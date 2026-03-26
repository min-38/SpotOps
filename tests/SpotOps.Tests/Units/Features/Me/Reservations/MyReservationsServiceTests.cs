using Microsoft.EntityFrameworkCore;
using SpotOps.Data;
using SpotOps.Features.Me.Reservations;
using SpotOps.Models;

namespace SpotOps.Tests.Units.Features.Me.Reservations;

public sealed class MyReservationsServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("my_res_" + Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task ListAsync_ReturnsLatestFirst_AndIncludesOptionalRelations()
    {
        await using var db = CreateDb();
        var (user, ev) = await SeedCoreAsync(db);

        var seat = new Seat
        {
            EventId = ev.Id,
            Section = "A",
            Row = "1",
            Number = "1",
            Status = SeatStatus.Sold
        };
        db.Seats.Add(seat);
        await db.SaveChangesAsync();

        var older = new Reservation
        {
            UserId = user.Id,
            EventId = ev.Id,
            Status = ReservationStatus.Cancelled,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5)
        };

        var newer = new Reservation
        {
            UserId = user.Id,
            EventId = ev.Id,
            SeatId = seat.Id,
            Status = ReservationStatus.Confirmed,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1),
            ExpiresAt = DateTime.UtcNow.AddMinutes(9)
        };

        db.Reservations.AddRange(older, newer);
        db.Payments.Add(new Payment
        {
            ReservationId = newer.Id,
            PortOnePaymentId = $"spotops-{newer.Id:N}",
            Amount = ev.Price,
            Status = PaymentStatus.Paid,
            PaidAt = DateTime.UtcNow
        });
        db.Tickets.Add(new Ticket
        {
            ReservationId = newer.Id,
            IsUsed = false,
            IssuedAt = DateTime.UtcNow.AddSeconds(-30)
        });
        await db.SaveChangesAsync();

        var svc = new MyReservationsService(db);

        var rows = await svc.ListAsync(user.Id, take: 50);

        Assert.Equal(2, rows.Count);
        Assert.Equal(newer.Id, rows[0].ReservationId);
        Assert.Equal(older.Id, rows[1].ReservationId);

        Assert.NotNull(rows[0].Event);
        Assert.Equal(ev.Id, rows[0].Event.Id);
        Assert.Equal(ev.Title, rows[0].Event.Title);

        Assert.NotNull(rows[0].Seat);
        Assert.Equal(seat.Id, rows[0].Seat!.Id);

        Assert.NotNull(rows[0].Payment);
        Assert.Equal($"spotops-{newer.Id:N}", rows[0].Payment!.PortOnePaymentId);
        Assert.Equal(PaymentStatus.Paid, rows[0].Payment!.Status);

        Assert.NotNull(rows[0].Ticket);
        Assert.False(rows[0].Ticket!.IsUsed);

        Assert.Null(rows[1].Seat);
        Assert.Null(rows[1].Payment);
        Assert.Null(rows[1].Ticket);
    }

    [Fact]
    public async Task ListAsync_ClampsTake()
    {
        await using var db = CreateDb();
        var (user, ev) = await SeedCoreAsync(db);

        for (var i = 0; i < 5; i++)
        {
            db.Reservations.Add(new Reservation
            {
                UserId = user.Id,
                EventId = ev.Id,
                Status = ReservationStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await db.SaveChangesAsync();

        var svc = new MyReservationsService(db);

        var defaultTake = await svc.ListAsync(user.Id, take: 0);
        Assert.Equal(5, defaultTake.Count);

        var tooBig = await svc.ListAsync(user.Id, take: 10_000);
        Assert.Equal(5, tooBig.Count);
    }

    [Fact]
    public async Task CancelAsync_ConfirmedPaidReservation_RefundsByPolicy_AndReleasesSeat()
    {
        await using var db = CreateDb();
        var (user, ev) = await SeedCoreAsync(db);
        ev.EventAt = DateTime.UtcNow.AddDays(8);
        await db.SaveChangesAsync();

        var seat = new Seat
        {
            EventId = ev.Id,
            Section = "A",
            Row = "1",
            Number = "9",
            Status = SeatStatus.Sold
        };
        db.Seats.Add(seat);
        await db.SaveChangesAsync();

        var reservation = new Reservation
        {
            UserId = user.Id,
            EventId = ev.Id,
            SeatId = seat.Id,
            Status = ReservationStatus.Confirmed,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync();

        db.Payments.Add(new Payment
        {
            ReservationId = reservation.Id,
            PortOnePaymentId = $"spotops-{reservation.Id:N}",
            Amount = 40000,
            Status = PaymentStatus.Paid
        });
        await db.SaveChangesAsync();

        var svc = new MyReservationsService(db);
        var (result, code, _) = await svc.CancelAsync(user.Id, reservation.Id);

        Assert.Null(code);
        Assert.NotNull(result);
        Assert.Equal(ReservationStatus.Cancelled, result!.Status);
        Assert.Equal(1.0m, result.RefundRate);
        Assert.Equal(40000m, result.RefundAmount);

        var savedReservation = await db.Reservations.SingleAsync(r => r.Id == reservation.Id);
        var savedSeat = await db.Seats.SingleAsync(s => s.Id == seat.Id);
        var savedPayment = await db.Payments.SingleAsync(p => p.ReservationId == reservation.Id);

        Assert.Equal(ReservationStatus.Cancelled, savedReservation.Status);
        Assert.Equal(SeatStatus.Available, savedSeat.Status);
        Assert.Equal(PaymentStatus.Refunded, savedPayment.Status);
    }

    [Fact]
    public async Task CancelAsync_ThreeToSevenDaysBeforeEvent_ReturnsFiftyPercentRefund()
    {
        await using var db = CreateDb();
        var (user, ev) = await SeedCoreAsync(db);
        ev.EventAt = DateTime.UtcNow.AddDays(4);
        await db.SaveChangesAsync();

        var reservation = new Reservation
        {
            UserId = user.Id,
            EventId = ev.Id,
            Status = ReservationStatus.Confirmed
        };
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync();

        db.Payments.Add(new Payment
        {
            ReservationId = reservation.Id,
            PortOnePaymentId = $"spotops-{reservation.Id:N}",
            Amount = 30000,
            Status = PaymentStatus.Paid
        });
        await db.SaveChangesAsync();

        var svc = new MyReservationsService(db);
        var (result, _, _) = await svc.CancelAsync(user.Id, reservation.Id);

        Assert.NotNull(result);
        Assert.Equal(0.5m, result!.RefundRate);
        Assert.Equal(15000m, result.RefundAmount);
    }

    [Fact]
    public async Task CancelAsync_AlreadyCancelled_ReturnsError()
    {
        await using var db = CreateDb();
        var (user, ev) = await SeedCoreAsync(db);

        var reservation = new Reservation
        {
            UserId = user.Id,
            EventId = ev.Id,
            Status = ReservationStatus.Cancelled
        };
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync();

        var svc = new MyReservationsService(db);
        var (result, code, _) = await svc.CancelAsync(user.Id, reservation.Id);

        Assert.Null(result);
        Assert.Equal("ME_RESERVATION_ALREADY_CANCELLED", code);
    }

    private static async Task<(User user, Event ev)> SeedCoreAsync(AppDbContext db)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = new User { Email = $"u_{suffix}@t.com", PasswordHash = "x", Name = "U" };
        var orgUser = new User { Email = $"o_{suffix}@t.com", PasswordHash = "x", Name = "O", Role = UserRole.Organizer };
        var org = new Organizer { UserId = orgUser.Id, BusinessNumber = $"7{suffix}7", CompanyName = "C" };
        var ev = new Event
        {
            OrganizerId = org.Id,
            Title = "Show",
            TicketType = TicketType.Seated,
            TotalCapacity = 10,
            Price = 45000,
            VenueName = "Hall",
            EventAt = DateTime.UtcNow.AddDays(3),
            SaleStartAt = DateTime.UtcNow.AddDays(-1),
            SaleEndAt = DateTime.UtcNow.AddDays(2)
        };

        db.Users.AddRange(user, orgUser);
        db.Organizers.Add(org);
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        return (user, ev);
    }
}

