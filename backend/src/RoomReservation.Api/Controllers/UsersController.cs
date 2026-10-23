using Microsoft.AspNetCore.Mvc;
using RoomReservation.Api.Dtos;
using RoomReservation.Api.Services;

namespace RoomReservation.Api.Controllers;

[Route("api/users")]
public class UsersController(IUserService users) : ApiControllerBase
{
    /// <summary>Lists users with the number of their active (booked, not yet finished) reservations.</summary>
    [HttpGet]
    public Task<IReadOnlyList<UserDto>> GetUsers(CancellationToken ct) => users.GetUsersAsync(ct);
}
