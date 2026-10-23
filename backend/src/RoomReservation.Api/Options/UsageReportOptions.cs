namespace RoomReservation.Api.Options;

/// <summary>Settings for the room usage report, bound from the "UsageReport" section of appsettings.json.</summary>
public class UsageReportOptions
{
    public const string SectionName = "UsageReport";

    /// <summary>A room is flagged when less than this share of its booked time was actually used.</summary>
    public int LowUsagePercent { get; set; } = 60;

    /// <summary>Rooms with fewer bookings than this are not flagged: a couple of bookings say little.</summary>
    public int MinBookings { get; set; } = 5;

    /// <summary>Period length when no start date is given.</summary>
    public int DefaultPeriodDays { get; set; } = 30;
}
