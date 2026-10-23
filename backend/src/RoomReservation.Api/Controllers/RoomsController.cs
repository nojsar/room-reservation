using Microsoft.AspNetCore.Mvc;
using RoomReservation.Api.Dtos;
using RoomReservation.Api.Services;

namespace RoomReservation.Api.Controllers;

[Route("api/rooms")]
public class RoomsController(IRoomService rooms) : ApiControllerBase
{
    /// <summary>Lists rooms. Filters: minCapacity, floor, and equipment (repeat it to require several items).</summary>
    [HttpGet]
    public Task<IReadOnlyList<RoomDto>> GetRooms([FromQuery] RoomFilter filter, CancellationToken ct) =>
        rooms.GetRoomsAsync(filter, ct);

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoomDto>> GetRoom(int id, CancellationToken ct) =>
        FromResult(await rooms.GetRoomAsync(id, ct));

    /// <summary>The floors that have rooms, for the floor filter.</summary>
    [HttpGet("floors")]
    public Task<IReadOnlyList<int>> GetFloors(CancellationToken ct) => rooms.GetFloorsAsync(ct);
}
