using RoomReservation.Api.Common;

namespace RoomReservation.Api.Booking.Rules;

/// <summary>R4: the end must be after the start, and the start must be in the future.</summary>
public sealed class ValidTimeRangeRule : IBookingRule
{
    public Error? Check(BookingContext context)
    {
        if (context.Request.End <= context.Request.Start)
        {
            return new Error("END_NOT_AFTER_START", "The end time must be after the start time.");
        }

        if (context.Request.Start <= context.Now)
        {
            return new Error("START_NOT_IN_FUTURE", "The start time must be in the future.");
        }

        return null;
    }
}
