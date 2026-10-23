using Microsoft.EntityFrameworkCore;
using RoomReservation.Api.Common;
using RoomReservation.Api.Data;
using RoomReservation.Api.Dtos;
using RoomReservation.Api.Mapping;

namespace RoomReservation.Api.Services;

public interface IRoomService
{
    Task<IReadOnlyList<RoomDto>> GetRoomsAsync(RoomFilter filter, CancellationToken ct);
    Task<Result<RoomDto>> GetRoomAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<int>> GetFloorsAsync(CancellationToken ct);
}

public class RoomService(AppDbContext db) : IRoomService
{
    public async Task<IReadOnlyList<RoomDto>> GetRoomsAsync(RoomFilter filter, CancellationToken ct)
    {
        var query = db.Rooms.AsNoTracking();

        if (filter.MinCapacity is int minCapacity)
        {
            query = query.Where(r => r.Capacity >= minCapacity);
        }

        if (filter.Floor is int floor)
        {
            query = query.Where(r => r.Floor == floor);
        }

        if (filter.Equipment is { Count: > 0 })
        {
            // A room matches when it has every requested item: all requested bits are set.
            var required = filter.Equipment.Combine();
            query = query.Where(r => (r.Equipment & required) == required);
        }

        var rooms = await query.OrderBy(r => r.Floor).ThenBy(r => r.Name).ToListAsync(ct);
        return rooms.Select(r => r.ToDto()).ToList();
    }

    public async Task<Result<RoomDto>> GetRoomAsync(int id, CancellationToken ct)
    {
        var room = await db.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);
        return room is null
            ? Error.NotFound("ROOM_NOT_FOUND", $"Room {id} does not exist.")
            : room.ToDto();
    }

    public async Task<IReadOnlyList<int>> GetFloorsAsync(CancellationToken ct) =>
        await db.Rooms.Select(r => r.Floor).Distinct().OrderBy(f => f).ToListAsync(ct);
}
