using RoomReservation.Api.Domain;
using RoomReservation.Api.Options;

namespace RoomReservation.Api.Booking;

/// <summary>
/// Everything a booking rule needs, loaded once by the validator. Rules only read this object
/// and never touch the database, so each rule is a small pure function that is easy to test.
/// </summary>
public sealed class BookingContext
{
    public required BookingRequest Request { get; init; }
    public required Room Room { get; init; }
    public required User Organizer { get; init; }
    public required DateTimeOffset Now { get; init; }
    public required BookingOptions Options { get; init; }

    /// <summary>The room's reservations that are not cancelled, without the one being edited.</summary>
    public required IReadOnlyList<Reservation> RoomReservations { get; init; }

    /// <summary>The organizer's reservations that are not cancelled, without the one being edited.</summary>
    public required IReadOnlyList<Reservation> OrganizerReservations { get; init; }
}
