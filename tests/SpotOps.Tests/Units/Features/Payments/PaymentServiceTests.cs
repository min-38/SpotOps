using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SpotOps.Data;
using SpotOps.Features.Payments;
using SpotOps.Features.Events.Reserve;
using SpotOps.Infrastructure.PortOne;
using SpotOps.Models;

namespace SpotOps.Tests.Units.Features.Payments;

public class PaymentServiceTests
{
    private sealed class StubPortOneApi : IPortOnePaymentApi
    {
        public JsonDocument? Next { get; set; }

        public Task<JsonDocument?> GetPaymentAsync(string paymentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Next);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("pay_" + Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(User user, Event ev, Reservation res)> SeedPendingReservationCore(AppDbContext db)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = new User { Email = $"u_{suffix}@t.com", PasswordHash = "x", Name = "U" };
        var orgUser = new User { Email = $"o_{suffix}@t.com", PasswordHash = "x", Name = "O", Role = UserRole.Organizer };
        var org = new Organizer { UserId = orgUser.Id, BusinessNumber = $"7{suffix}7", CompanyName = "C" };
        var ev = new Event
        {
            OrganizerId = org.Id,
            Title = "S",
            TicketType = TicketType.Seated,
            TotalCapacity = 10,
            Price = 45000,
            VenueName = "V",
            EventAt = DateTime.UtcNow.AddDays(3),
            SaleStartAt = DateTime.UtcNow.AddDays(-1),
            SaleEndAt = DateTime.UtcNow.AddDays(2)
        };
        var seat = new Seat { EventId = ev.Id, Section = "A", Row = "1", Number = "1", Status = SeatStatus.Available };
        db.Users.AddRange(user, orgUser);
        db.Organizers.Add(org);
        db.Events.Add(ev);
        db.Seats.Add(seat);
        await db.SaveChangesAsync();
        var reserve = new ReserveService(db);
        var (reservation, err) = await reserve.ReserveAsync(ev.Id, user.Id, seat.Id);
        if (err != null || reservation == null)
            throw new InvalidOperationException(err ?? "fail");
        return (user, ev, reservation);
    }

    [Fact]
    public async Task Prepare_CreatesPaymentWithStablePortOnePaymentId()
    {
        await using var ctx = CreateDb();
        var (user, _, res) = await SeedPendingReservationCore(ctx);
        var stub = new StubPortOneApi();
        var opt = Options.Create(new PortOneOptions { StoreId = "store-1", ApiSecret = "s" });
        var svc = new PaymentService(ctx, stub, opt);

        var (prep, err) = await svc.PrepareAsync(user.Id, res.Id);
        Assert.Null(err);
        Assert.NotNull(prep);
        Assert.Equal($"spotops-{res.Id:N}", prep!.PaymentId);
        Assert.Equal("store-1", prep.StoreId);
        Assert.Equal(45000, prep.TotalAmount);

        var pay = await ctx.Payments.SingleAsync();
        Assert.Equal(prep.PaymentId, pay.PortOnePaymentId);
    }

    [Fact]
    public async Task Webhook_TransactionPaid_ConfirmsReservationAndSeatSold()
    {
        await using var ctx = CreateDb();
        var (user, _, res) = await SeedPendingReservationCore(ctx);
        var stub = new StubPortOneApi();
        var paymentId = $"spotops-{res.Id:N}";
        stub.Next = JsonDocument.Parse(
            "{\"id\":\"" + paymentId + "\",\"status\":\"PAID\",\"amount\":{\"total\":45000}}");

        var opt = Options.Create(new PortOneOptions { StoreId = "my-store", ApiSecret = "x" });
        var svc = new PaymentService(ctx, stub, opt);

        var (prep, perr) = await svc.PrepareAsync(user.Id, res.Id);
        Assert.Null(perr);
        Assert.NotNull(prep);

        var webhook =
            "{\"type\":\"Transaction.Paid\",\"timestamp\":\"2024-04-25T10:00:00.000Z\",\"data\":{\"paymentId\":\"" +
            paymentId +
            "\",\"storeId\":\"my-store\",\"transactionId\":\"tx-1\"}}";
        await svc.ProcessPortOneWebhookAsync(webhook);

        var pay = await ctx.Payments.SingleAsync();
        Assert.Equal(PaymentStatus.Paid, pay.Status);
        Assert.Equal("tx-1", pay.PgTransactionId);

        var resRow = await ctx.Reservations.Include(x => x.Seat).Include(x => x.Ticket).SingleAsync(x => x.Id == res.Id);
        Assert.Equal(ReservationStatus.Confirmed, resRow.Status);
        Assert.Equal(SeatStatus.Sold, resRow.Seat!.Status);
        Assert.NotNull(resRow.Ticket);
    }
}
