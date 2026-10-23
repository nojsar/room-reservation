using Microsoft.Extensions.Time.Testing;
using RoomReservation.Api.Data;
using RoomReservation.Api.Options;
using RoomReservation.Api.Services;

namespace RoomReservation.Tests;

public class RoomUsageReportServiceTests
{
    private static readonly DateTimeOffset Now = TestDb.Now;

    private static RoomUsageReportService Service(AppDbContext db, int minBookings = 2) =>
        new(db,
            Microsoft.Extensions.Options.Options.Create(new UsageReportOptions { LowUsagePercent = 60, MinBookings = minBookings }),
            new FakeTimeProvider(Now));

    [Fact]
    public async Task Report_CountsCancelledAsUnusedAndEarlyEndsUntilTheyEnded()
    {
        using var db = TestDb.CreateWithRoomsAndUsers();
        var dayAgo = Now.AddDays(-1);

        var cancelled = TestDb.Reservation(1, 1, dayAgo, TimeSpan.FromHours(2));
        cancelled.CancelledAt = dayAgo.AddHours(-3);
        var endedEarly = TestDb.Reservation(1, 2, dayAgo.AddHours(3), TimeSpan.FromHours(2));
        endedEarly.EndedEarlyAt = endedEarly.Start.AddMinutes(30);
        var fullyUsed = TestDb.Reservation(2, 1, dayAgo, TimeSpan.FromHours(1));
        db.Reservations.AddRange(cancelled, endedEarly, fullyUsed);
        db.SaveChanges();

        var report = await Service(db).GetReportAsync(Now.AddDays(-7), Now, default);
        var small = report.Rooms.Single(r => r.RoomName == "Small");
        var medium = report.Rooms.Single(r => r.RoomName == "Medium");

        Assert.Equal(4, small.BookedHours);
        Assert.Equal(0.5, small.UsedHours);
        Assert.Equal(12.5, small.UsagePercent);
        Assert.Equal(1, small.Cancellations);
        Assert.Equal(1, small.EndedEarly);
        Assert.True(small.IsOftenBookedButRarelyUsed);

        Assert.Equal(100, medium.UsagePercent);
        Assert.False(medium.IsOftenBookedButRarelyUsed);

        // Flagged rooms come first.
        Assert.Equal("Small", report.Rooms[0].RoomName);
    }

    [Fact]
    public async Task Report_DoesNotFlagRoomsWithFewBookings()
    {
        using var db = TestDb.CreateWithRoomsAndUsers();
        var cancelled = TestDb.Reservation(1, 1, Now.AddDays(-1), TimeSpan.FromHours(1));
        cancelled.CancelledAt = Now.AddDays(-2);
        db.Reservations.Add(cancelled);
        db.SaveChanges();

        var report = await Service(db, minBookings: 5).GetReportAsync(null, null, default);

        Assert.False(report.Rooms.Single(r => r.RoomName == "Small").IsOftenBookedButRarelyUsed);
    }

    [Fact]
    public async Task Report_IgnoresTheFutureAndCountsOngoingMeetingsOnlyUntilNow()
    {
        using var db = TestDb.CreateWithRoomsAndUsers();
        db.Reservations.Add(TestDb.Reservation(1, 1, Now.AddMinutes(-30), TimeSpan.FromHours(2))); // ongoing
        db.Reservations.Add(TestDb.Reservation(1, 1, Now.AddDays(1), TimeSpan.FromHours(2)));      // future
        db.SaveChanges();

        var report = await Service(db).GetReportAsync(Now.AddDays(-1), Now.AddDays(7), default);
        var small = report.Rooms.Single(r => r.RoomName == "Small");

        Assert.Equal(Now, report.To);
        Assert.Equal(1, small.Bookings);
        Assert.Equal(0.5, small.BookedHours);
        Assert.Equal(0.5, small.UsedHours);
    }

    [Fact]
    public async Task Report_RoomWithoutBookingsHasNoUsagePercent()
    {
        using var db = TestDb.CreateWithRoomsAndUsers();

        var report = await Service(db).GetReportAsync(null, null, default);

        Assert.All(report.Rooms, row => Assert.Null(row.UsagePercent));
        Assert.Equal(Now.AddDays(-30), report.From);
    }
}
