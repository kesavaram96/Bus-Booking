using BusBooking.API.Extensions;
using BusBooking.Application.Common.Models;
using BusBooking.Application.Tickets.DTOs;
using BusBooking.Application.Tickets.Queries.GetTicketsByBooking;
using BusBooking.Application.Tickets.Queries.VerifyTicket;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.API.Controllers;

[ApiController]
[Route("api/tickets")]
public class TicketsController : ControllerBase
{
    private readonly ISender _sender;

    public TicketsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Public like Booking/Payment's own Create — whoever holds a Booking's Guid
    /// already has full access to it elsewhere in this API, so fetching its tickets (to
    /// display/print/download) needs no further proof of ownership.</summary>
    [HttpGet("booking/{bookingId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TicketDto>>>> GetByBooking(Guid bookingId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTicketsByBookingQuery(bookingId), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TicketDto>>.SuccessResponse(result));
    }

    /// <summary>The doc's "only authorized staff can verify tickets" — the one genuinely
    /// security-sensitive ticket operation, since it's what decides whether someone boards.</summary>
    [HttpGet("verify/{ticketCode}")]
    [Authorize(Policy = AuthorizationPolicies.RequireBookingStaff)]
    public async Task<ActionResult<ApiResponse<TicketVerificationDto>>> Verify(string ticketCode, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new VerifyTicketQuery(ticketCode), cancellationToken);
        return Ok(ApiResponse<TicketVerificationDto>.SuccessResponse(result));
    }
}
