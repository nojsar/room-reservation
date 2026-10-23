using RoomReservation.Api.Common;

namespace RoomReservation.Api.Booking.Rules;

/// <summary>R1: a single reservation cannot last longer than the configured number of days (exactly that long is allowed).</summary>
public sealed class MaxDurationRule : IBookingRule
{
    public Error? Check(BookingContext context)
    {
        var maxDuration = TimeSpan.FromDays(context.Options.MaxDurationDays);
        var duration = context.Request.End - context.Request.Start;

        return duration > maxDuration
            ? new Error("MAX_DURATION_EXCEEDED", $"A reservation can last at most {context.Options.MaxDurationDays} days.")
            : null;
    }
}
