using Microsoft.EntityFrameworkCore;
using RoomReservation.Api.Data;
using RoomReservation.Api.Domain;
using RoomReservation.Api.Dtos;
using RoomReservation.Api.Mapping;

namespace RoomReservation.Api.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken ct);
}

public class UserService(AppDbContext db, TimeProvider timeProvider) : IUserService
{
    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        var users = await db.Users
            .AsNoTracking()
            .Include(u => u.Reservations)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(ct);

        // "Active" means still booked: not cancelled and not over yet (meetings in progress count).
        return users
            .Select(u => u.ToDto(u.Reservations.Count(r => r.GetStatus(now) == ReservationStatus.Booked)))
            .ToList();
    }
}
