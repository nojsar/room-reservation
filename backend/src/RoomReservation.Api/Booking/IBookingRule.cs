using RoomReservation.Api.Common;

namespace RoomReservation.Api.Booking;

/// <summary>
/// One booking rule (R1 to R7). Each rule is its own class behind this interface (the Strategy
/// pattern): the validator runs whatever rules are registered, so adding a rule means adding a
/// class and one line in Program.cs, without changing the existing rules.
/// </summary>
public interface IBookingRule
{
    /// <summary>Returns an error when the request breaks the rule, otherwise null.</summary>
    Error? Check(BookingContext context);
}
