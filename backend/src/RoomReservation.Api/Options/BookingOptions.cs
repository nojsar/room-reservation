namespace RoomReservation.Api.Options;

/// <summary>
/// Booking limits, bound from the "Booking" section of appsettings.json (the Options pattern),
/// so they can be changed without touching the code.
/// </summary>
public class BookingOptions
{
    public const string SectionName = "Booking";

    /// <summary>R1: a single reservation cannot be longer than this. Exactly this long is allowed.</summary>
    public int MaxDurationDays { get; set; } = 7;

    /// <summary>R6: a reservation cannot start more than this many days in the future.</summary>
    public int MaxDaysAhead { get; set; } = 90;
}
