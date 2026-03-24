using Microsoft.EntityFrameworkCore;
using SpotOps.Data;
using SpotOps.Features.Events.Queue;
using SpotOps.Features.Events.Reserve;
using SpotOps.Features.Events.Selection;
using SpotOps.Models;

namespace SpotOps.Tests.Units.Features.Events.Selection;

public class SelectionServiceTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("selection_" + Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    // 성공: 좌석 레이아웃 조회
    [Fact]
    public async Task GetLayout_WithValidSession_ReturnsSeats()
    {
        await using var db = CreateInMemoryDb();
        var (user, ev, seat) = await SeedSeatedEventAsync(db);
        var queue = new QueueService();
        var selection = new SelectionService(db, queue, new ReserveService(db));

        var join = queue.Join(ev.Id, user.Id);
        queue.InviteNextBatch(ev.Id, 1, 300);
        var status = queue.GetStatus(ev.Id, join.QueueEntryId);
        Assert.True(status.Invited);
        Assert.NotNull(status.SessionToken);

        var (layout, error) = await selection.GetLayoutAsync(ev.Id, user.Id, status.SessionToken);
        Assert.Null(error);
        Assert.NotNull(layout);
        Assert.True(layout!.IsSeated);
        Assert.Single(layout.Seats);
        Assert.Equal(seat.Id, layout.Seats[0].Id);
    }

    // 성공: 좌석 홀드
    [Fact]
    public async Task Hold_WithValidSession_ReservesSeat()
    {
        await using var db = CreateInMemoryDb();
        var (user, ev, seat) = await SeedSeatedEventAsync(db);
        var queue = new QueueService();
        var selection = new SelectionService(db, queue, new ReserveService(db));

        var join = queue.Join(ev.Id, user.Id);
        queue.InviteNextBatch(ev.Id, 1, 300);
        var status = queue.GetStatus(ev.Id, join.QueueEntryId);

        var (res, err) = await selection.HoldAsync(ev.Id, user.Id, status.SessionToken, seat.Id);
        Assert.Null(err);
        Assert.NotNull(res);

        var seatRow = await db.Seats.SingleAsync(s => s.Id == seat.Id);
        Assert.Equal(SeatStatus.Reserved, seatRow.Status);
    }

    // 실패: 유효하지 않은 세션 토큰
    [Fact]
    public async Task GetLayout_WithInvalidSession_ReturnsError()
    {
        await using var db = CreateInMemoryDb();
        var (user, ev, _) = await SeedSeatedEventAsync(db);
        var queue = new QueueService();
        var selection = new SelectionService(db, queue, new ReserveService(db));

        var (layout, error) = await selection.GetLayoutAsync(ev.Id, user.Id, "bad-token");
        Assert.NotNull(error);
        Assert.Null(layout);
    }

    // 실패: 유효하지 않은 세션 토큰
    private static async Task<(User user, Event ev, Seat seat)> SeedSeatedEventAsync(AppDbContext db)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = new User
        {
            Email = $"buyer_{suffix}@test.com",
            PasswordHash = "x",
            Name = "Buyer"
        };
        var organizerUser = new User
        {
            Email = $"org_{suffix}@test.com",
            PasswordHash = "x",
            Name = "Org",
            Role = UserRole.Organizer
        };
        var organizer = new Organizer
        {
            UserId = organizerUser.Id,
            BusinessNumber = $"9{suffix}12",
            CompanyName = "Co"
        };
        var ev = new Event
        {
            OrganizerId = organizer.Id,
            Title = "Show",
            TicketType = TicketType.Seated,
            TotalCapacity = 100,
            Price = 10000,
            VenueName = "Hall",
            EventAt = DateTime.UtcNow.AddDays(7),
            SaleStartAt = DateTime.UtcNow.AddDays(-1),
            SaleEndAt = DateTime.UtcNow.AddDays(1)
        };
        var seat = new Seat
        {
            EventId = ev.Id,
            Section = "A",
            Row = "1",
            Number = "1",
            Status = SeatStatus.Available
        };

        db.Users.AddRange(user, organizerUser);
        db.Organizers.Add(organizer);
        db.Events.Add(ev);
        db.Seats.Add(seat);
        await db.SaveChangesAsync();

        return (user, ev, seat);
    }
}
