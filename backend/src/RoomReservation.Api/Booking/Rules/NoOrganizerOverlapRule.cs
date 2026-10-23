using RoomReservation.Api.Common;
using RoomReservation.Api.Mapping;

namespace RoomReservation.Api.Booking.Rules;

/// <summary>R3: the same person cannot organize two meetings that overlap, even in different rooms.</summary>
public sealed class NoOrganizerOverlapRule : IBookingRule
{
    public Error? Check(BookingContext context)
    {
        var clash = context.OrganizerReservations
            .Where(r => r.Overlaps(context.Request.Start, context.Request.End))
            .OrderBy(r => r.Start)
            .FirstOrDefault();

        return clash is null
            ? null
            : new Error(
                "ORGANIZER_ALREADY_BOOKED",
                $"{context.Organizer.FullName()} already organizes \"{clash.Title}\" at that time.");
    }
}
