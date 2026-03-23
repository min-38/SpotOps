using Microsoft.EntityFrameworkCore;
using SpotOps.Data;
using SpotOps.Features.Events.Reserve;
using SpotOps.Models;
using Testcontainers.PostgreSql;

namespace SpotOps.Tests.Integrations.Features.Events.Reserve;

public class ReserveServiceTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public ReserveServiceTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    private AppDbContext CreateDb() => _fixture.CreateDb();

    private (AppDbContext db, Event ev, User user) CreateFixture(
        TicketType ticketType = TicketType.Standing,
        int capacity = 10)
    {
        var db = CreateDb();

        var suffix = Guid.NewGuid().ToString("N");

        var user = new User
        {
            Email = $"test-{suffix}@test.com",
            PasswordHash = "hash",
            Name = "테스트유저",
            Role = UserRole.Buyer
        };

        var organizer = new Organizer
        {
            UserId = user.Id,
            BusinessNumber = $"12345{Random.Shared.Next(100000, 999999)}",
            CompanyName = "테스트기획사"
        };

        var ev = new Event
        {
            OrganizerId = organizer.Id,
            Title = "테스트공연",
            TicketType = ticketType,
            TotalCapacity = capacity,
            Price = 50000,
            VenueName = "테스트공연장",
            EventAt = DateTime.UtcNow.AddDays(7),
            SaleStartAt = DateTime.UtcNow.AddDays(-1),
            SaleEndAt = DateTime.UtcNow.AddDays(1)
        };

        db.Users.Add(user);
        db.Organizers.Add(organizer);
        db.Events.Add(ev);
        db.SaveChanges();

        return (db, ev, user);
    }

    // 성공: 선착순 정상 예약
    [Fact]
    public async Task Reserve_Standing_Succeeds()
    {
        var (db, ev, user) = CreateFixture();
        var service = new ReserveService(db);

        var (reservation, error) = await service.ReserveAsync(ev.Id, user.Id, null);

        Assert.NotNull(reservation);
        Assert.Null(error);
        Assert.Equal(ReservationStatus.Pending, reservation.Status);
    }

    // 실패: 판매 기간 외 예약
    [Fact]
    public async Task Reserve_OutsideSalePeriod_ReturnsError()
    {
        var (db, ev, user) = CreateFixture();
        ev.SaleStartAt = DateTime.UtcNow.AddDays(1);
        ev.SaleEndAt = DateTime.UtcNow.AddDays(2);
        db.SaveChanges();

        var service = new ReserveService(db);
        var (reservation, error) = await service.ReserveAsync(ev.Id, user.Id, null);

        Assert.Null(reservation);
        Assert.NotNull(error);
    }

    // 실패: 중복 예약
    [Fact]
    public async Task Reserve_DuplicateReservation_ReturnsError()
    {
        var (db, ev, user) = CreateFixture();
        var service = new ReserveService(db);

        await service.ReserveAsync(ev.Id, user.Id, null);
        var (reservation, error) = await service.ReserveAsync(ev.Id, user.Id, null);

        Assert.Null(reservation);
        Assert.NotNull(error);
    }

    // 실패: 매진
    [Fact]
    public async Task Reserve_SoldOut_ReturnsError()
    {
        var (db, ev, user) = CreateFixture(capacity: 1);
        var service = new ReserveService(db);

        var otherUser = new User
        {
            Email = $"other-{Guid.NewGuid():N}@test.com",
            PasswordHash = "hash",
            Name = "다른유저",
            Role = UserRole.Buyer
        };
        db.Users.Add(otherUser);
        db.SaveChanges();

        await service.ReserveAsync(ev.Id, otherUser.Id, null);
        var (reservation, error) = await service.ReserveAsync(ev.Id, user.Id, null);

        Assert.Null(reservation);
        Assert.NotNull(error);
    }

    // 성공: 좌석 지정 정상 예약
    [Fact]
    public async Task Reserve_Seated_WithValidSeat_Succeeds()
    {
        var (db, ev, user) = CreateFixture(TicketType.Seated);

        var seat = new Seat
        {
            EventId = ev.Id,
            Section = "A",
            Row = "1",
            Number = "1",
            Status = SeatStatus.Available
        };
        db.Seats.Add(seat);
        db.SaveChanges();

        var service = new ReserveService(db);
        var (reservation, error) = await service.ReserveAsync(ev.Id, user.Id, seat.Id);

        Assert.NotNull(reservation);
        Assert.Null(error);
    }

    // 실패: 이미 선택된 좌석 예약
    [Fact]
    public async Task Reserve_Seated_WithUnavailableSeat_ReturnsError()
    {
        var (db, ev, user) = CreateFixture(TicketType.Seated);

        var seat = new Seat
        {
            EventId = ev.Id,
            Section = "A",
            Row = "1",
            Number = "1",
            Status = SeatStatus.Reserved
        };
        db.Seats.Add(seat);
        db.SaveChanges();

        var service = new ReserveService(db);
        var (reservation, error) = await service.ReserveAsync(ev.Id, user.Id, seat.Id);

        Assert.Null(reservation);
        Assert.NotNull(error);
    }
}
