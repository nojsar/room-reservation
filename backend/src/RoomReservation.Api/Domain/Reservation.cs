using RoomReservation.Api.Common;

namespace RoomReservation.Api.Domain;

public class Reservation
{
    public int Id { get; set; }

    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public int OrganizerId { get; set; }
    public User Organizer { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public int Attendees { get; set; }

    /// <summary>Set when the reservation is cancelled. A cancelled reservation no longer blocks the room.</summary>
    public DateTimeOffset? CancelledAt { get; set; }

    /// <summary>Set when the meeting is ended before its planned end. The room is free from this moment.</summary>
    public DateTimeOffset? EndedEarlyAt { get; set; }

    /// <summary>When the room is actually free again: the early end if there was one, otherwise the planned end.</summary>
    public DateTimeOffset EffectiveEnd => EndedEarlyAt ?? End;

    /// <summary>
    /// The status is derived, not stored: a booked meeting becomes Completed by itself once its time
    /// has passed, so nothing has to update the database when the clock moves on.
    /// </summary>
    public ReservationStatus GetStatus(DateTimeOffset now)
    {
        if (CancelledAt is not null)
        {
            return ReservationStatus.Cancelled;
        }

        return EffectiveEnd <= now ? ReservationStatus.Completed : ReservationStatus.Booked;
    }

    public bool HasStarted(DateTimeOffset now) => Start <= now;

    /// <summary>True when this reservation holds the time between start (inclusive) and end (exclusive).
    /// Meetings that touch, like 9:00-10:00 and 10:00-11:00, do not overlap.</summary>
    public bool Overlaps(DateTimeOffset start, DateTimeOffset end) => Start < end && start < EffectiveEnd;

    /// <summary>Upcoming booked meetings can be changed or cancelled.</summary>
    public bool IsUpcoming(DateTimeOffset now) => GetStatus(now) == ReservationStatus.Booked && !HasStarted(now);

    /// <summary>A booked meeting that has started and not ended yet.</summary>
    public bool IsInProgress(DateTimeOffset now) => GetStatus(now) == ReservationStatus.Booked && HasStarted(now);

    public Error? Cancel(DateTimeOffset now)
    {
        if (GetStatus(now) != ReservationStatus.Booked)
        {
            return new Error("RESERVATION_NOT_ACTIVE", "Only booked reservations can be cancelled.");
        }

        if (HasStarted(now))
        {
            return new Error("MEETING_ALREADY_STARTED", "This meeting has already started. End it early instead of cancelling it.");
        }

        CancelledAt = now;
        return null;
    }

    public Error? EndEarly(DateTimeOffset now)
    {
        if (!IsInProgress(now))
        {
            return new Error("MEETING_NOT_IN_PROGRESS", "Only a meeting that is in progress can be ended early.");
        }

        EndedEarlyAt = now;
        return null;
    }
}
