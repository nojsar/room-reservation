using RoomReservation.Api.Common;

namespace RoomReservation.Api.Booking.Rules;

/// <summary>
/// R2: a room cannot have two active reservations at the same time. Cancelled reservations are
/// already left out of the context, and a meeting that ended early only blocks the room until it ended.
/// </summary>
public sealed class NoRoomOverlapRule : IBookingRule
{
    public Error? Check(BookingContext context)
    {
        var clash = context.RoomReservations
            .Where(r => r.Overlaps(context.Request.Start, context.Request.End))
            .OrderBy(r => r.Start)
            .FirstOrDefault();

        return clash is null
            ? null
            : new Error("ROOM_ALREADY_BOOKED", $"{context.Room.Name} is already booked at that time for \"{clash.Title}\".");
    }
}
