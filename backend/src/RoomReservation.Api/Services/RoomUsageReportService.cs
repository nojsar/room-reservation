using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoomReservation.Api.Data;
using RoomReservation.Api.Domain;
using RoomReservation.Api.Dtos;
using RoomReservation.Api.Options;

namespace RoomReservation.Api.Services;

public interface IRoomUsageReportService
{
    Task<RoomUsageReportDto> GetReportAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);
}

/// <summary>
/// Compares booked time with the time rooms were actually used. Cancellations and meetings that
/// end early are the signals we have, so a room with many of them is "often booked but rarely used".
/// Only the past is measured: what happens in the future is not known yet.
/// </summary>
public class RoomUsageReportService(
    AppDbContext db,
    IOptions<UsageReportOptions> options,
    TimeProvider timeProvider) : IRoomUsageReportService
{
    public async Task<RoomUsageReportDto> GetReportAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        var settings = options.Value;
        var now = timeProvider.GetUtcNow();
        var periodEnd = to is null || to > now ? now : to.Value;
        var periodStart = from ?? periodEnd.AddDays(-settings.DefaultPeriodDays);

        var rooms = await db.Rooms.AsNoTracking().ToListAsync(ct);
        var reservations = await db.Reservations
            .AsNoTracking()
            .Where(r => r.Start >= periodStart && r.Start < periodEnd)
            .ToListAsync(ct);

        var rows = rooms
            .Select(room => BuildRow(room, reservations.Where(r => r.RoomId == room.Id).ToList(), periodEnd, settings))
            .OrderByDescending(row => row.IsOftenBookedButRarelyUsed)
            .ThenByDescending(row => row.BookedHours - row.UsedHours)
            .ThenBy(row => row.RoomName)
            .ToList();

        return new RoomUsageReportDto(periodStart, periodEnd, settings.LowUsagePercent, settings.MinBookings, rows);
    }

    private static RoomUsageDto BuildRow(Room room, List<Reservation> reservations, DateTimeOffset periodEnd, UsageReportOptions settings)
    {
        // Time is counted up to the end of the period, so a meeting in progress counts only its first part.
        double HoursUntil(DateTimeOffset start, DateTimeOffset end) =>
            Math.Max(0, ((end < periodEnd ? end : periodEnd) - start).TotalHours);

        var bookedHours = reservations.Sum(r => HoursUntil(r.Start, r.End));
        var usedHours = reservations.Where(r => r.CancelledAt is null).Sum(r => HoursUntil(r.Start, r.EffectiveEnd));
        double? usagePercent = bookedHours > 0 ? Math.Round(usedHours / bookedHours * 100, 1) : null;

        var isFlagged = reservations.Count >= settings.MinBookings
                        && usagePercent is double percent
                        && percent < settings.LowUsagePercent;

        return new RoomUsageDto(
            room.Id,
            room.Name,
            room.Floor,
            room.IsActive,
            Bookings: reservations.Count,
            Cancellations: reservations.Count(r => r.CancelledAt is not null),
            EndedEarly: reservations.Count(r => r.EndedEarlyAt is not null),
            BookedHours: Math.Round(bookedHours, 1),
            UsedHours: Math.Round(usedHours, 1),
            UsagePercent: usagePercent,
            IsOftenBookedButRarelyUsed: isFlagged);
    }
}
