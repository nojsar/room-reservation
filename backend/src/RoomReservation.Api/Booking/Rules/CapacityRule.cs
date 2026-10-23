using RoomReservation.Api.Common;

namespace RoomReservation.Api.Booking.Rules;

/// <summary>R5: the number of attendees cannot exceed the room capacity.</summary>
public sealed class CapacityRule : IBookingRule
{
    public Error? Check(BookingContext context)
    {
        return context.Request.Attendees > context.Room.Capacity
            ? new Error(
                "CAPACITY_EXCEEDED",
                $"{context.Room.Name} fits {context.Room.Capacity} people, but {context.Request.Attendees} attendees were requested.")
            : null;
    }
}
