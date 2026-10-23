using RoomReservation.Api.Common;

namespace RoomReservation.Api.Booking.Rules;

/// <summary>R6: a reservation cannot start more than the configured number of days in the future.</summary>
public sealed class BookingHorizonRule : IBookingRule
{
    public Error? Check(BookingContext context)
    {
        var latestStart = context.Now.AddDays(context.Options.MaxDaysAhead);

        return context.Request.Start > latestStart
            ? new Error(
                "BEYOND_BOOKING_HORIZON",
                $"A reservation can start at most {context.Options.MaxDaysAhead} days from now.")
            : null;
    }
}
