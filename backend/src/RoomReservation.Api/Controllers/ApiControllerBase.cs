using Microsoft.AspNetCore.Mvc;
using RoomReservation.Api.Common;
using RoomReservation.Api.Dtos;

namespace RoomReservation.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Turns a service result into an HTTP response: the value on success, 404 when something
    /// does not exist, 422 when a business rule is broken. Errors always have the same shape.
    /// </summary>
    protected ActionResult<T> FromResult<T>(Result<T> result, Func<T, ActionResult<T>>? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            return onSuccess is null ? Ok(result.Value) : onSuccess(result.Value!);
        }

        var body = new ErrorResponse(result.Errors.Select(e => new ErrorDto(e.Code, e.Message)).ToList());
        return result.Errors.Any(e => e.Type == ErrorType.NotFound)
            ? NotFound(body)
            : UnprocessableEntity(body);
    }
}
