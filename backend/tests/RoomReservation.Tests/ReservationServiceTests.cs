using Microsoft.Extensions.Time.Testing;
using RoomReservation.Api.Booking;
using RoomReservation.Api.Booking.Rules;
using RoomReservation.Api.Data;
using RoomReservation.Api.Domain;
using RoomReservation.Api.Dtos;
using RoomReservation.Api.Options;
using RoomReservation.Api.Services;

namespace RoomReservation.Tests;

/// <summary>The service with the real validator and all rules, on an in-memory database.</summary>
public class ReservationServiceTests
{
    private static readonly DateTimeOffset Now = TestDb.Now;
    private static readonly DateTimeOffset Tomorrow9 = new(Now.Date.AddDays(1).AddHours(9), TimeSpan.Zero);

    private static ReservationService Service(AppDbContext db, BookingOptions? options = null)
    {
        var clock = new FakeTimeProvider(Now);
        IBookingRule[] rules =
        {
            new MaxDurationRule(), new NoRoomOverlapRule(), new NoOrganizerOverlapRule(), new ValidTimeRangeRule(),
            new CapacityRule(), new BookingHorizonRule(), new ActiveRoomRule(),
        };
        var validator = new BookingValidator(db, rules, Microsoft.Extensions.Options.Options.Create(options ?? new BookingOptions()), clock);
        return new ReservationService(db, validator, clock);
    }

    private static SaveReservationRequest Request(int roomId, int organizerId, DateTimeOffset start, TimeSpan length, int attendees = 2) => new()
    {
        RoomId = roomId,
        OrganizerId = organizerId,
        Title = "Planning",
        Start = start,
        End = start + length,
        Attendees = attendees,
    };

    private static Reservation Add(AppDbContext db, Reservation reservation)
    {
        db.Reservations.Add(reservation);
        db.SaveChanges();
        return reservation;
    }

    [Fact]
    public async Task Create_ValidRequest_SavesAndReturnsBookedReservation()
    {
        using var db = TestDb.CreateWithRoomsAndUsers();

        var result = await Service(db).CreateAsync(Request(1, 1, Tomorrow9, TimeSpan.FromHours(1)), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReservationStatus.Booked, result.Value!.Status);
        Assert.Equal("Small", result.Value.RoomName);
        Assert.True(result.Value.CanCancel);
        Assert.Single(db.Reservations);
    }

    [Fact]
    public async Task Create_BreakingSeveralRules_ReturnsEveryErrorAndSavesNothing()
    {
        using var db = TestDb.CreateWithRoomsAndUsers();

        // Room 4 is inactive and fits 10 people; the start is in the past.
        var result = await Service(db).CreateAsync(Request(4, 1, Now.AddHours(-1), TimeSpan.FromHours(1), attendees: 12), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            new[] { "START_NOT_IN_FUTURE", "CAPACITY_EXCEEDED", "ROOM_INACTIVE" },
            result.Errors.Select(e => e.Code));
        Assert.Empty(db.Reservations);
    }

    [Fact]
    public async Task Create_UnknownRoomAndUser_ReturnsNotFoundCodes()
    {
        using var db = TestDb.CreateWithRoomsAndUsers();

        var result = await Service(db).CreateAsync(Request(99, 99, Tomorrow9, TimeSpan.FromHours(1)), default);

        Assert.Equal(new[] { "ROOM_NOT_FOUND", "USER_NOT_FOUND" }, result.Errors.Select(e => e.Code));
    }

    [Fact]
    public async Task Create_CancelledReservationDoesNotBlockTheRoom()
    {
        using var db = TestDb.CreateWithRoomsAndUsers();
        var cancelled = TestDb.Reservation(1, 2, Tomorrow9, TimeSpan.FromHours(1));
        cancelled.CancelledAt = Now;
        Add(db, cancelled);

        var result = await Service(db).CreateAsync(Request(1, 1, Tomorrow9, TimeSpan.FromHours(1)), default);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Create_RoomAlreadyBooked_IsRejected()
    {
        using var db = TestDb.CreateWithRoomsAndUsers();
        Add(db, TestDb.Reservation(1, 2, Tomorrow9, TimeSpan.FromHours(1)));

        var result = await Service(db).CreateAsync(Request(1, 1, Tomorrow9.AddMinutes(30), TimeSpan.FromHours(1)), default);

        Assert.Equal("ROOM_ALREADY_BOOKED", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Update_DoesNotClashWithItself()
    {
        using var db = TestDb.CreateWithRoomsAndUsers();
        var existing = Add(db, TestDb.Reservation(1, 1, Tomorrow9, TimeSpan.FromHours(1)));

        // Make the same meeting 30 minutes longer.
        var result = await Service(db).UpdateAsync(existing.Id, Request(1, 1, Tomorrow9, TimeSpan.FromMinutes(90)), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(Tomorrow9.AddMinutes(90), result.Value!.End);
    }

    [Fact]
    public async Task Update_PastReservation_IsRejected()
    {
        using var db = TestDb.CreateWithRoomsAndUsers();
        var past = Add(db, TestDb.Reservation(1, 1, Now.AddDays(-1), TimeSpan.FromHours(1)));

        var result = await Service(db).UpdateAsync(past.Id, Request(1, 1, Tomorrow9, TimeSpan.FromHours(1)), default);

        Assert.Equal("RESERVATION_NOT_EDITABLE", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Cancel_UpcomingReservation_SetsStatusCancelled()
    {
        using var db = TestDb.CreateWithRoomsAndUsers();
        var upcoming = Add(db, TestDb.Reservation(1, 1, Tomorrow9, TimeSpan.FromHours(1)));

        var result = await Service(db).CancelAsync(upcoming.Id, default);

        Assert.Equal(ReservationStatus.Cancelled, result.Value!.Status);
        Assert.Equal(Now, result.Value.CancelledAt);
        Assert.False(result.Value.CanCancel);
    }

    [Fact]
    public async Task Cancel_MeetingInProgress_IsRejected()
    {
        using var db = TestDb.CreateWithRoomsAndUsers();
        var ongoing = Add(db, TestDb.Reservation(1, 1, Now.AddMinutes(-10), TimeSpan.FromHours(1)));

        var result = await Service(db).CancelAsync(ongoing.Id, default);

        Assert.Equal("MEETING_ALREADY_STARTED", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task EndEarly_MeetingInProgress_CompletesItNow()
    {
        using var db = TestDb.CreateWithRoomsAndUsers();
        var ongoing = Add(db, TestDb.Reservation(1, 1, Now.AddMinutes(-10), TimeSpan.FromHours(1)));

        var result = await Service(db).EndEarlyAsync(ongoing.Id, default);

        Assert.Equal(ReservationStatus.Completed, result.Value!.Status);
        Assert.Equal(Now, result.Value.EndedEarlyAt);
    }

    [Fact]
    public async Task EndEarly_MeetingNotStarted_IsRejected()
    {
        using var db = TestDb.CreateWithRoomsAndUsers();
        var upcoming = Add(db, TestDb.Reservation(1, 1, Tomorrow9, TimeSpan.FromHours(1)));

        var result = await Service(db).EndEarlyAsync(upcoming.Id, default);

        Assert.Equal("MEETING_NOT_IN_PROGRESS", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task GetReservations_FiltersByStatusOrganizerAndDateRange()
    {
        using var db = TestDb.CreateWithRoomsAndUsers();
        Add(db, TestDb.Reservation(1, 1, Now.AddDays(-2), TimeSpan.FromHours(1)));   // completed
        Add(db, TestDb.Reservation(1, 1, Tomorrow9, TimeSpan.FromHours(1)));          // booked, tomorrow
        Add(db, TestDb.Reservation(2, 1, Now.AddDays(5), TimeSpan.FromHours(1)));     // booked, later
        Add(db, TestDb.Reservation(3, 2, Tomorrow9, TimeSpan.FromHours(1)));          // booked, other organizer
        var service = Service(db);

        var completed = await service.GetReservationsAsync(new ReservationFilter { Status = ReservationStatus.Completed }, default);
        var mineBooked = await service.GetReservationsAsync(new ReservationFilter { OrganizerId = 1, Status = ReservationStatus.Booked }, default);
        var nextTwoDays = await service.GetReservationsAsync(new ReservationFilter { From = Now, To = Now.AddDays(2) }, default);

        Assert.Single(completed);
        Assert.Equal(2, mineBooked.Count);
        Assert.Equal(2, nextTwoDays.Count);
    }
}
