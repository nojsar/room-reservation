namespace RoomReservation.Api.Booking;

/// <summary>A new reservation, or the new values of an existing one, waiting to be checked against the rules.</summary>
/// <param name="ReservationId">Set when an existing reservation is edited, so it does not clash with itself.</param>
public sealed record BookingRequest(
    int RoomId,
    int OrganizerId,
    DateTimeOffset Start,
    DateTimeOffset End,
    int Attendees,
    int? ReservationId = null);
