using BusBooking.API.Extensions;
using BusBooking.Application.Common.Models;
using BusBooking.Application.Reports.DTOs;
using BusBooking.Application.Reports.Queries.GetCancellationReport;
using BusBooking.Application.Reports.Queries.GetCustomerBookingHistory;
using BusBooking.Application.Reports.Queries.GetDailyBookingReport;
using BusBooking.Application.Reports.Queries.GetPickupPointPassengerReport;
using BusBooking.Application.Reports.Queries.GetRevenueReport;
using BusBooking.Application.Reports.Queries.GetSeatOccupancyReport;
using BusBooking.Application.Reports.Queries.GetTripPassengerReport;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.API.Controllers;

/// <summary>Every report here is staff-only (RequireBookingStaff) and returns the full filtered
/// dataset rather than a page — the doc's own framing ("designed for future React dashboards
/// and Excel/PDF export") means the whole point is exporting everything matching the filters,
/// the same reasoning GetPassengerManifest (Phase 16) already used.</summary>
[ApiController]
[Route("api/reports")]
[Authorize(Policy = AuthorizationPolicies.RequireBookingStaff)]
public class ReportsController : ControllerBase
{
    private readonly ISender _sender;

    public ReportsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("daily-bookings")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DailyBookingReportEntryDto>>>> GetDailyBookings(
        [FromQuery] GetDailyBookingReportQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DailyBookingReportEntryDto>>.SuccessResponse(result));
    }

    [HttpGet("trip-passengers")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PassengerReportEntryDto>>>> GetTripPassengers(
        [FromQuery] GetTripPassengerReportQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PassengerReportEntryDto>>.SuccessResponse(result));
    }

    [HttpGet("revenue")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RevenueReportEntryDto>>>> GetRevenue(
        [FromQuery] GetRevenueReportQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RevenueReportEntryDto>>.SuccessResponse(result));
    }

    [HttpGet("cancellations")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CancellationReportEntryDto>>>> GetCancellations(
        [FromQuery] GetCancellationReportQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CancellationReportEntryDto>>.SuccessResponse(result));
    }

    [HttpGet("seat-occupancy")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SeatOccupancyReportEntryDto>>>> GetSeatOccupancy(
        [FromQuery] GetSeatOccupancyReportQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SeatOccupancyReportEntryDto>>.SuccessResponse(result));
    }

    [HttpGet("customer-history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomerBookingHistoryEntryDto>>>> GetCustomerHistory(
        [FromQuery] GetCustomerBookingHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CustomerBookingHistoryEntryDto>>.SuccessResponse(result));
    }

    [HttpGet("pickup-points")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PassengerReportEntryDto>>>> GetPickupPointPassengers(
        [FromQuery] GetPickupPointPassengerReportQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PassengerReportEntryDto>>.SuccessResponse(result));
    }
}
