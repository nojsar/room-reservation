using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using RoomReservation.Api.Data;
using RoomReservation.Api.Domain;

namespace RoomReservation.Tests;

/// <summary>
/// The demo data is generated, so these tests check that it respects the same rules
/// the API will enforce for new bookings.
/// </summary>
public class DbSeederTests
{
    // A fixed Wednesday at 12:00 UTC, so the result does not depend on when the tests run.
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    private static AppDbContext SeededDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        DbSeeder.Seed(db, new FakeTimeProvider(Now));
        return db;
    }

    [Fact]
    public void Seed_CreatesRoomsUsersAndReservations()
    {
        using var db = SeededDb();

        Assert.Equal(8, db.Rooms.Count());
        Assert.Equal(8, db.Users.Count());
        Assert.True(db.Reservations.Count() > 50);
        Assert.Contains(db.Rooms, r => !r.IsActive);
    }

    [Fact]
    public void Seed_HasPastCurrentAndFutureData()
    {
        using var db = SeededDb();
        var statuses = db.Reservations.AsEnumerable().Select(r => r.GetStatus(Now)).ToList();

        Assert.Contains(ReservationStatus.Completed, statuses);
        Assert.Contains(ReservationStatus.Booked, statuses);
        Assert.Contains(ReservationStatus.Cancelled, statuses);
        Assert.Contains(db.Reservations, r => r.EndedEarlyAt != null);
    }

    [Fact]
    public void Seed_ActiveReservationsNeverOverlapInTheSameRoom()
    {
        using var db = SeededDb();
        var active = db.Reservations.Where(r => r.CancelledAt == null).ToList();

        foreach (var group in active.GroupBy(r => r.RoomId))
        {
            AssertNoOverlaps(group.ToList());
        }
    }

    [Fact]
    public void Seed_OrganizersNeverHaveOverlappingMeetings()
    {
        using var db = SeededDb();
        var active = db.Reservations.Where(r => r.CancelledAt == null).ToList();

        foreach (var group in active.GroupBy(r => r.OrganizerId))
        {
            AssertNoOverlaps(group.ToList());
        }
    }

    [Fact]
    public void Seed_AttendeesNeverExceedRoomCapacity()
    {
        using var db = SeededDb();

        Assert.All(db.Reservations.Include(r => r.Room), r => Assert.InRange(r.Attendees, 1, r.Room.Capacity));
    }

    [Fact]
    public void Seed_InactiveRoomHasNoFutureBookings()
    {
        using var db = SeededDb();
        var inactiveRoomIds = db.Rooms.Where(r => !r.IsActive).Select(r => r.Id).ToList();

        Assert.DoesNotContain(db.Reservations, r => inactiveRoomIds.Contains(r.RoomId) && r.Start > Now);
    }

    private static void AssertNoOverlaps(List<Reservation> reservations)
    {
        var ordered = reservations.OrderBy(r => r.Start).ToList();
        for (var i = 1; i < ordered.Count; i++)
        {
            Assert.True(ordered[i - 1].End <= ordered[i].Start,
                $"Reservations {ordered[i - 1].Id} and {ordered[i].Id} overlap");
        }
    }
}
