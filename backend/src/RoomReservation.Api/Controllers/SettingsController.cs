using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RoomReservation.Api.Options;

namespace RoomReservation.Api.Controllers;

[Route("api/settings")]
public class SettingsController(IOptions<BookingOptions> bookingOptions) : ApiControllerBase
{
    /// <summary>The configured booking limits, so the form can guide the user. The back end still checks every rule.</summary>
    [HttpGet("booking")]
    public BookingOptions GetBookingSettings() => bookingOptions.Value;
}
