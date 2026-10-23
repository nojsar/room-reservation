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
}
