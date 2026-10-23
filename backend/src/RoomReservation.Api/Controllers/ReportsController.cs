using Microsoft.AspNetCore.Mvc;
using RoomReservation.Api.Dtos;
using RoomReservation.Api.Services;

namespace RoomReservation.Api.Controllers;

[Route("api/reports")]
public class ReportsController(IRoomUsageReportService usageReport) : ApiControllerBase
{
    /// <summary>
    /// Booked versus actually used time per room. Without dates the report covers the last 30 days;
    /// a "to" date in the future is cut to now, because future usage is not known yet.
    /// </summary>
    [HttpGet("room-usage")]
    public Task<RoomUsageReportDto> GetRoomUsage([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken ct) =>
        usageReport.GetReportAsync(from, to, ct);
}
