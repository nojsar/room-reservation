namespace RoomReservation.Api.Dtos;

public sealed record RoomUsageReportDto(
    DateTimeOffset From,
    DateTimeOffset To,
    int LowUsagePercent,
    int MinBookings,
    IReadOnlyList<RoomUsageDto> Rooms);

/// <summary>How one room was booked and actually used in the report period.</summary>
/// <param name="BookedHours">Time reserved, including bookings that were later cancelled.</param>
/// <param name="UsedHours">Time the meetings really took: cancelled bookings count as zero, meetings that ended early count until they ended.</param>
/// <param name="UsagePercent">UsedHours as a share of BookedHours; null when the room had no bookings.</param>
public sealed record RoomUsageDto(
    int RoomId,
    string RoomName,
    int Floor,
    bool IsActive,
    int Bookings,
    int Cancellations,
    int EndedEarly,
    double BookedHours,
    double UsedHours,
    double? UsagePercent,
    bool IsOftenBookedButRarelyUsed);
