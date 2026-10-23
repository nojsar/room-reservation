using RoomReservation.Api.Common;

namespace RoomReservation.Api.Booking.Rules;

/// <summary>R7: inactive rooms (for example under renovation) cannot be booked.</summary>
public sealed class ActiveRoomRule : IBookingRule
{
    public Error? Check(BookingContext context)
    {
        return context.Room.IsActive
            ? null
            : new Error("ROOM_INACTIVE", $"{context.Room.Name} is inactive and cannot be booked.");
    }
}
