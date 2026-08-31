using BusBooking.API.Extensions;
using BusBooking.Application.Audit.DTOs;
using BusBooking.Application.Audit.Queries.GetAuditLogs;
using BusBooking.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.API.Controllers;

/// <summary>RequireAdminOrAbove, deliberately stricter than the RequireBookingStaff gate every
/// other business-data endpoint uses — an audit trail exposes what every role (including staff
/// themselves) has been doing across the whole system, a different sensitivity level than the
/// booking/payment/passenger data BookingStaff already sees day to day.</summary>
[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = AuthorizationPolicies.RequireAdminOrAbove)]
public class AuditLogsController : ControllerBase
{
    private readonly ISender _sender;

    public AuditLogsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<AuditLogDto>>>> GetAuditLogs(
        [FromQuery] GetAuditLogsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(ApiResponse<PaginatedList<AuditLogDto>>.SuccessResponse(result));
    }
}
