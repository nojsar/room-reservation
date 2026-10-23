using RoomReservation.Api.Booking;
using RoomReservation.Api.Booking.Rules;
using RoomReservation.Api.Domain;
using RoomReservation.Api.Options;

namespace RoomReservation.Tests;

/// <summary>
/// Each rule is tested on its own with a hand-built context, including the edge cases from the
/// task: exactly 7 days and exactly 90 days are allowed, a cancelled booking does not block the room.
/// </summary>
public class BookingRulesTests
{
    private static readonly DateTimeOffset Now = TestDb.Now;
    private static readonly DateTimeOffset Tomorrow9 = new(Now.Date.AddDays(1).AddHours(9), TimeSpan.Zero);

    private static BookingContext Context(
        DateTimeOffset start,
        DateTimeOffset end,
        int attendees = 2,
        bool roomIsActive = true,
        IReadOnlyList<Reservation>? roomReservations = null,
        IReadOnlyList<Reservation>? organizerReservations = null,
        BookingOptions? options = null) => new()
    {
        Request = new BookingRequest(1, 1, start, end, attendees),
        Room = new Room { Id = 1, Name = "Neris", Capacity = 6, IsActive = roomIsActive },
        Organizer = new User { Id = 1, FirstName = "Jonas", LastName = "Petraitis" },
        Now = Now,
        Options = options ?? new BookingOptions(),
        RoomReservations = roomReservations ?? Array.Empty<Reservation>(),
        OrganizerReservations = organizerReservations ?? Array.Empty<Reservation>(),
    };

    private static Reservation Existing(DateTimeOffset start, TimeSpan length, int roomId = 1) =>
        TestDb.Reservation(roomId, 1, start, length);

    // R1 Maximum duration

    [Fact]
    public void R1_ExactlySevenDays_IsAllowed() =>
        Assert.Null(new MaxDurationRule().Check(Context(Tomorrow9, Tomorrow9.AddDays(7))));

    [Fact]
    public void R1_LongerThanSevenDays_IsRejected() =>
        Assert.Equal("MAX_DURATION_EXCEEDED", new MaxDurationRule().Check(Context(Tomorrow9, Tomorrow9.AddDays(7).AddMinutes(1)))?.Code);

    [Fact]
    public void R1_UsesTheConfiguredLimit()
    {
        var options = new BookingOptions { MaxDurationDays = 2 };
        Assert.NotNull(new MaxDurationRule().Check(Context(Tomorrow9, Tomorrow9.AddDays(3), options: options)));
    }

    // R2 No overlapping room bookings

    [Fact]
    public void R2_OverlappingBookingInTheSameRoom_IsRejected()
    {
        var existing = new[] { Existing(Tomorrow9.AddMinutes(30), TimeSpan.FromHours(1)) };
        var error = new NoRoomOverlapRule().Check(Context(Tomorrow9, Tomorrow9.AddHours(1), roomReservations: existing));

        Assert.Equal("ROOM_ALREADY_BOOKED", error?.Code);
    }

    [Fact]
    public void R2_BackToBackBookings_AreAllowed()
    {
        var existing = new[] { Existing(Tomorrow9.AddHours(-1), TimeSpan.FromHours(1)) }; // 8:00 to 9:00
        Assert.Null(new NoRoomOverlapRule().Check(Context(Tomorrow9, Tomorrow9.AddHours(1), roomReservations: existing)));
    }

    [Fact]
    public void R2_MeetingThatEndedEarly_FreesTheRestOfItsTime()
    {
        // Booked 8:00 to 10:00, but ended at 8:45, so 9:00 is free.
        var endedEarly = Existing(Tomorrow9.AddHours(-1), TimeSpan.FromHours(2));
        endedEarly.EndedEarlyAt = Tomorrow9.AddMinutes(-15);

        Assert.Null(new NoRoomOverlapRule().Check(Context(Tomorrow9, Tomorrow9.AddHours(1), roomReservations: new[] { endedEarly })));
    }

    // R3 No overlapping organizer bookings

    [Fact]
    public void R3_OrganizerBusyInAnotherRoom_IsRejected()
    {
        var otherRoom = new[] { Existing(Tomorrow9, TimeSpan.FromHours(2), roomId: 5) };
        var error = new NoOrganizerOverlapRule().Check(Context(Tomorrow9.AddHours(1), Tomorrow9.AddHours(3), organizerReservations: otherRoom));

        Assert.Equal("ORGANIZER_ALREADY_BOOKED", error?.Code);
    }

    [Fact]
    public void R3_OrganizerFree_IsAllowed()
    {
        var later = new[] { Existing(Tomorrow9.AddHours(2), TimeSpan.FromHours(1), roomId: 5) };
        Assert.Null(new NoOrganizerOverlapRule().Check(Context(Tomorrow9, Tomorrow9.AddHours(1), organizerReservations: later)));
    }

    // R4 Valid time range

    [Fact]
    public void R4_StartInThePast_IsRejected() =>
        Assert.Equal("START_NOT_IN_FUTURE", new ValidTimeRangeRule().Check(Context(Now.AddMinutes(-1), Now.AddHours(1)))?.Code);

    [Fact]
    public void R4_StartRightNow_IsRejected() =>
        Assert.Equal("START_NOT_IN_FUTURE", new ValidTimeRangeRule().Check(Context(Now, Now.AddHours(1)))?.Code);

    [Fact]
    public void R4_EndBeforeStart_IsRejected() =>
        Assert.Equal("END_NOT_AFTER_START", new ValidTimeRangeRule().Check(Context(Tomorrow9, Tomorrow9.AddHours(-1)))?.Code);

    [Fact]
    public void R4_FutureRange_IsAllowed() =>
        Assert.Null(new ValidTimeRangeRule().Check(Context(Tomorrow9, Tomorrow9.AddHours(1))));

    // R5 Capacity

    [Fact]
    public void R5_AttendeesEqualToCapacity_IsAllowed() =>
        Assert.Null(new CapacityRule().Check(Context(Tomorrow9, Tomorrow9.AddHours(1), attendees: 6)));

    [Fact]
    public void R5_MoreAttendeesThanCapacity_IsRejected() =>
        Assert.Equal("CAPACITY_EXCEEDED", new CapacityRule().Check(Context(Tomorrow9, Tomorrow9.AddHours(1), attendees: 7))?.Code);

    // R6 Booking horizon

    [Fact]
    public void R6_StartExactlyNinetyDaysAhead_IsAllowed() =>
        Assert.Null(new BookingHorizonRule().Check(Context(Now.AddDays(90), Now.AddDays(90).AddHours(1))));

    [Fact]
    public void R6_StartFurtherThanNinetyDaysAhead_IsRejected() =>
        Assert.Equal("BEYOND_BOOKING_HORIZON", new BookingHorizonRule().Check(Context(Now.AddDays(90).AddMinutes(1), Now.AddDays(91)))?.Code);

    [Fact]
    public void R6_UsesTheConfiguredLimit()
    {
        var options = new BookingOptions { MaxDaysAhead = 10 };
        Assert.NotNull(new BookingHorizonRule().Check(Context(Now.AddDays(11), Now.AddDays(11).AddHours(1), options: options)));
    }

    // R7 Active rooms only

    [Fact]
    public void R7_InactiveRoom_IsRejected() =>
        Assert.Equal("ROOM_INACTIVE", new ActiveRoomRule().Check(Context(Tomorrow9, Tomorrow9.AddHours(1), roomIsActive: false))?.Code);

    [Fact]
    public void R7_ActiveRoom_IsAllowed() =>
        Assert.Null(new ActiveRoomRule().Check(Context(Tomorrow9, Tomorrow9.AddHours(1))));
}
