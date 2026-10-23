using Microsoft.AspNetCore.Mvc;
using RoomReservation.Api.Dtos;
using RoomReservation.Api.Services;

namespace RoomReservation.Api.Controllers;

[Route("api/reservations")]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
public class ReservationsController(IReservationService reservations) : ApiControllerBase
{
    /// <summary>Lists reservations. Filters: roomId, organizerId, status, and a date range (from, to).</summary>
    [HttpGet]
    public Task<IReadOnlyList<ReservationDto>> GetReservations([FromQuery] ReservationFilter filter, CancellationToken ct) =>
        reservations.GetReservationsAsync(filter, ct);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReservationDto>> GetReservation(int id, CancellationToken ct) =>
        FromResult(await reservations.GetReservationAsync(id, ct));

    /// <summary>Creates a reservation. Every booking rule is checked; broken rules come back as 422 with all errors.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<ReservationDto>> Create(SaveReservationRequest request, CancellationToken ct) =>
        FromResult(
            await reservations.CreateAsync(request, ct),
            created => CreatedAtAction(nameof(GetReservation), new { id = created.Id }, created));

    /// <summary>Changes an upcoming reservation. The new values are checked against every booking rule.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ReservationDto>> Update(int id, SaveReservationRequest request, CancellationToken ct) =>
        FromResult(await reservations.UpdateAsync(id, request, ct));

    /// <summary>Cancels a reservation that has not started yet. The room becomes free again.</summary>
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<ReservationDto>> Cancel(int id, CancellationToken ct) =>
        FromResult(await reservations.CancelAsync(id, ct));

    /// <summary>Ends a meeting that is in progress now. The room is free from this moment.</summary>
    [HttpPost("{id:int}/end")]
    public async Task<ActionResult<ReservationDto>> EndEarly(int id, CancellationToken ct) =>
        FromResult(await reservations.EndEarlyAsync(id, ct));
}
