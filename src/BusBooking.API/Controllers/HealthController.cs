using BusBooking.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.API.Controllers;

/// <summary>
/// Basic liveness endpoint used to verify the solution scaffold builds and runs end to end.
/// Not a business feature — full health checks (DB/Redis connectivity) land in Phase 22.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<object>> Get()
    {
        var payload = new
        {
            status = "Healthy",
            timestampUtc = DateTime.UtcNow
        };

        return Ok(ApiResponse<object>.SuccessResponse(payload, "BusBooking API is running."));
    }
}
