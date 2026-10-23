using Microsoft.EntityFrameworkCore;
using RoomReservation.Api.Booking;
using RoomReservation.Api.Common;
using RoomReservation.Api.Data;
using RoomReservation.Api.Domain;
using RoomReservation.Api.Dtos;
using RoomReservation.Api.Mapping;

namespace RoomReservation.Api.Services;

public interface IReservationService
{
    Task<IReadOnlyList<ReservationDto>> GetReservationsAsync(ReservationFilter filter, CancellationToken ct);
    Task<Result<ReservationDto>> GetReservationAsync(int id, CancellationToken ct);
    Task<Result<ReservationDto>> CreateAsync(SaveReservationRequest request, CancellationToken ct);
    Task<Result<ReservationDto>> UpdateAsync(int id, SaveReservationRequest request, CancellationToken ct);
    Task<Result<ReservationDto>> CancelAsync(int id, CancellationToken ct);
    Task<Result<ReservationDto>> EndEarlyAsync(int id, CancellationToken ct);
}

public class ReservationService(
    AppDbContext db,
    IBookingValidator validator,
    TimeProvider timeProvider) : IReservationService
{
    public async Task<IReadOnlyList<ReservationDto>> GetReservationsAsync(ReservationFilter filter, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        var query = db.Reservations.AsNoTracking().Include(r => r.Room).Include(r => r.Organizer).AsQueryable();

        if (filter.RoomId is int roomId)
        {
            query = query.Where(r => r.RoomId == roomId);
        }

        if (filter.OrganizerId is int organizerId)
        {
            query = query.Where(r => r.OrganizerId == organizerId);
        }

        // Date range: keep reservations that overlap the period.
        if (filter.From is DateTimeOffset from)
        {
            query = query.Where(r => r.End > from);
        }

        if (filter.To is DateTimeOffset to)
        {
            query = query.Where(r => r.Start < to);
        }

        var reservations = await query.OrderBy(r => r.Start).ThenBy(r => r.Id).ToListAsync(ct);

        // The status depends on the current time, so it is filtered after loading, using the same
        // GetStatus method everywhere. With a real database this would move into the query.
        if (filter.Status is ReservationStatus status)
        {
            reservations = reservations.Where(r => r.GetStatus(now) == status).ToList();
        }

        return reservations.Select(r => r.ToDto(now)).ToList();
    }

    public async Task<Result<ReservationDto>> GetReservationAsync(int id, CancellationToken ct)
    {
        var reservation = await LoadAsync(id, ct);
        return reservation is null ? NotFound(id) : reservation.ToDto(timeProvider.GetUtcNow());
    }

    public async Task<Result<ReservationDto>> CreateAsync(SaveReservationRequest request, CancellationToken ct)
    {
        var errors = await validator.ValidateAsync(ToBookingRequest(request), ct);
        if (errors.Count > 0)
        {
            return Result<ReservationDto>.Failure(errors);
        }

        var reservation = new Reservation();
        Apply(request, reservation);
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync(ct);

        return (await GetReservationAsync(reservation.Id, ct));
    }

    public async Task<Result<ReservationDto>> UpdateAsync(int id, SaveReservationRequest request, CancellationToken ct)
    {
        var reservation = await db.Reservations.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (reservation is null)
        {
            return NotFound(id);
        }

        if (!reservation.IsUpcoming(timeProvider.GetUtcNow()))
        {
            return new Error("RESERVATION_NOT_EDITABLE", "Only upcoming booked reservations can be changed.");
        }

        var errors = await validator.ValidateAsync(ToBookingRequest(request, id), ct);
        if (errors.Count > 0)
        {
            return Result<ReservationDto>.Failure(errors);
        }

        Apply(request, reservation);
        await db.SaveChangesAsync(ct);

        return await GetReservationAsync(id, ct);
    }

    public Task<Result<ReservationDto>> CancelAsync(int id, CancellationToken ct) =>
        ChangeAsync(id, (reservation, now) => reservation.Cancel(now), ct);

    public Task<Result<ReservationDto>> EndEarlyAsync(int id, CancellationToken ct) =>
        ChangeAsync(id, (reservation, now) => reservation.EndEarly(now), ct);

    /// <summary>Loads a reservation, lets the domain object apply a change (cancel, end early) and saves it.</summary>
    private async Task<Result<ReservationDto>> ChangeAsync(
        int id,
        Func<Reservation, DateTimeOffset, Error?> change,
        CancellationToken ct)
    {
        var reservation = await LoadAsync(id, ct, tracked: true);
        if (reservation is null)
        {
            return NotFound(id);
        }

        var now = timeProvider.GetUtcNow();
        var error = change(reservation, now);
        if (error is not null)
        {
            return error;
        }

        await db.SaveChangesAsync(ct);
        return reservation.ToDto(now);
    }

    private Task<Reservation?> LoadAsync(int id, CancellationToken ct, bool tracked = false)
    {
        var query = db.Reservations.Include(r => r.Room).Include(r => r.Organizer).AsQueryable();
        if (!tracked)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    private static BookingRequest ToBookingRequest(SaveReservationRequest request, int? reservationId = null) =>
        new(request.RoomId, request.OrganizerId, request.Start!.Value, request.End!.Value, request.Attendees, reservationId);

    private static void Apply(SaveReservationRequest request, Reservation reservation)
    {
        reservation.RoomId = request.RoomId;
        reservation.OrganizerId = request.OrganizerId;
        reservation.Title = request.Title.Trim();
        reservation.Start = request.Start!.Value;
        reservation.End = request.End!.Value;
        reservation.Attendees = request.Attendees;
    }

    private static Error NotFound(int id) =>
        Error.NotFound("RESERVATION_NOT_FOUND", $"Reservation {id} does not exist.");
}
