using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoomReservation.Api.Common;
using RoomReservation.Api.Data;
using RoomReservation.Api.Options;

namespace RoomReservation.Api.Booking;

public interface IBookingValidator
{
    /// <summary>Checks a new or edited reservation against every booking rule and returns all broken rules.</summary>
    Task<IReadOnlyList<Error>> ValidateAsync(BookingRequest request, CancellationToken ct);
}

public class BookingValidator(
    AppDbContext db,
    IEnumerable<IBookingRule> rules,
    IOptions<BookingOptions> options,
    TimeProvider timeProvider) : IBookingValidator
{
    public async Task<IReadOnlyList<Error>> ValidateAsync(BookingRequest request, CancellationToken ct)
    {
        var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == request.RoomId, ct);
        var organizer = await db.Users.FirstOrDefaultAsync(u => u.Id == request.OrganizerId, ct);

        var errors = new List<Error>();
        if (room is null)
        {
            errors.Add(new Error("ROOM_NOT_FOUND", $"Room {request.RoomId} does not exist."));
        }

        if (organizer is null)
        {
            errors.Add(new Error("USER_NOT_FOUND", $"User {request.OrganizerId} does not exist."));
        }

        if (room is null || organizer is null)
        {
            return errors;
        }

        // Load the reservations the overlap rules need once: active ones of this room or this organizer.
        var related = await db.Reservations
            .AsNoTracking()
            .Where(r => r.CancelledAt == null
                        && r.Id != request.ReservationId
                        && (r.RoomId == request.RoomId || r.OrganizerId == request.OrganizerId))
            .ToListAsync(ct);

        var context = new BookingContext
        {
            Request = request,
            Room = room,
            Organizer = organizer,
            Now = timeProvider.GetUtcNow(),
            Options = options.Value,
            RoomReservations = related.Where(r => r.RoomId == request.RoomId).ToList(),
            OrganizerReservations = related.Where(r => r.OrganizerId == request.OrganizerId).ToList(),
        };

        // Run every rule and collect all errors, so the user can fix everything at once.
        return rules.Select(rule => rule.Check(context)).OfType<Error>().ToList();
    }
}
