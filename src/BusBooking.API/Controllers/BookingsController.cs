using System.Security.Claims;
using BusBooking.API.Extensions;
using BusBooking.Application.Bookings.Commands.CancelBooking;
using BusBooking.Application.Bookings.Commands.CreateBooking;
using BusBooking.Application.Bookings.DTOs;
using BusBooking.Application.Bookings.Queries.GetBookingById;
using BusBooking.Application.Bookings.Queries.GetBookings;
using BusBooking.Application.Common.Models;
using BusBooking.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.API.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private readonly ISender _sender;

    public BookingsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// One endpoint for all three actors (registered customer, guest, business staff) — per
    /// the doc, the same booking logic must never be duplicated between them. Public: a guest
    /// has no account, and staff authenticate with a non-Customer role, so this can't be
    /// gated behind [Authorize] without breaking the guest flow. CustomerId is always decided
    /// here from the JWT claim, never trusted from the request body.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Create(
        CreateBookingCommand command,
        CancellationToken cancellationToken)
    {
        Guid? customerId = null;
        if (User.Identity?.IsAuthenticated == true && User.IsInRole(Roles.Customer))
        {
            customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        var result = await _sender.Send(command with { CustomerId = customerId }, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            ApiResponse<BookingDto>.SuccessResponse(result, "Booking confirmed."));
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.RequireBookingStaff)]
    public async Task<ActionResult<ApiResponse<PaginatedList<BookingDto>>>> GetBookings(
        [FromQuery] GetBookingsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(ApiResponse<PaginatedList<BookingDto>>.SuccessResponse(result));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireBookingStaff)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetBookingByIdQuery(id), cancellationToken);
        return Ok(ApiResponse<BookingDto>.SuccessResponse(result));
    }

    /// <summary>
    /// Both actor paths the doc asks for share this one endpoint: staff can cancel any
    /// booking, a Customer can cancel only their own (checked in the handler, since that
    /// needs the loaded Booking). CancelledBy/IsStaffCancellation are always decided here from
    /// the JWT, never trusted from the body — the same rule as Create's CustomerId.
    /// </summary>
    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Policy = AuthorizationPolicies.RequireBookingStaffOrCustomer)]
    public async Task<ActionResult<ApiResponse<BookingDto>>> Cancel(
        Guid id,
        CancelBookingRequest request,
        CancellationToken cancellationToken)
    {
        var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isStaffCancellation = !User.IsInRole(Roles.Customer);

        var command = new CancelBookingCommand(id, request.CancellationReason, callerId, isStaffCancellation);
        var result = await _sender.Send(command, cancellationToken);

        return Ok(ApiResponse<BookingDto>.SuccessResponse(result, "Booking cancelled."));
    }
}
