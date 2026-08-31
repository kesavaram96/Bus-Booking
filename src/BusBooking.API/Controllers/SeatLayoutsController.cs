using BusBooking.API.Extensions;
using BusBooking.Application.Common.Models;
using BusBooking.Application.SeatLayouts.Commands.ActivateSeat;
using BusBooking.Application.SeatLayouts.Commands.AddSeat;
using BusBooking.Application.SeatLayouts.Commands.CreateSeatLayout;
using BusBooking.Application.SeatLayouts.Commands.DeactivateSeat;
using BusBooking.Application.SeatLayouts.Commands.RemoveSeat;
using BusBooking.Application.SeatLayouts.Commands.UpdateSeatLayout;
using BusBooking.Application.SeatLayouts.Commands.UpdateSeatNumber;
using BusBooking.Application.SeatLayouts.Commands.UpdateSeatPosition;
using BusBooking.Application.SeatLayouts.DTOs;
using BusBooking.Application.SeatLayouts.Queries.GetSeatLayoutById;
using BusBooking.Application.SeatLayouts.Queries.GetSeatLayouts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.API.Controllers;

[ApiController]
[Route("api/seat-layouts")]
[Authorize]
public class SeatLayoutsController : ControllerBase
{
    private readonly ISender _sender;

    public SeatLayoutsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.RequireBookingStaff)]
    public async Task<ActionResult<ApiResponse<PaginatedList<SeatLayoutSummaryDto>>>> GetSeatLayouts(
        [FromQuery] GetSeatLayoutsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(ApiResponse<PaginatedList<SeatLayoutSummaryDto>>.SuccessResponse(result));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireBookingStaff)]
    public async Task<ActionResult<ApiResponse<SeatLayoutDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSeatLayoutByIdQuery(id), cancellationToken);
        return Ok(ApiResponse<SeatLayoutDto>.SuccessResponse(result));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<SeatLayoutDto>>> Create(
        CreateSeatLayoutCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            ApiResponse<SeatLayoutDto>.SuccessResponse(result, "Seat layout created."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<SeatLayoutDto>>> Update(
        Guid id,
        UpdateSeatLayoutCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command with { Id = id }, cancellationToken);
        return Ok(ApiResponse<SeatLayoutDto>.SuccessResponse(result, "Seat layout updated."));
    }

    [HttpPost("{id:guid}/seats")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<SeatDto>>> AddSeat(
        Guid id,
        AddSeatCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command with { SeatLayoutId = id }, cancellationToken);
        return Ok(ApiResponse<SeatDto>.SuccessResponse(result, "Seat added."));
    }

    [HttpPut("{id:guid}/seats/{seatId:guid}/position")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<SeatDto>>> UpdateSeatPosition(
        Guid id,
        Guid seatId,
        UpdateSeatPositionCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command with { SeatLayoutId = id, SeatId = seatId }, cancellationToken);
        return Ok(ApiResponse<SeatDto>.SuccessResponse(result, "Seat position updated."));
    }

    [HttpPut("{id:guid}/seats/{seatId:guid}/number")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<SeatDto>>> UpdateSeatNumber(
        Guid id,
        Guid seatId,
        UpdateSeatNumberCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command with { SeatLayoutId = id, SeatId = seatId }, cancellationToken);
        return Ok(ApiResponse<SeatDto>.SuccessResponse(result, "Seat number updated."));
    }

    [HttpPatch("{id:guid}/seats/{seatId:guid}/activate")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<object>>> ActivateSeat(
        Guid id,
        Guid seatId,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new ActivateSeatCommand(id, seatId), cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Seat activated."));
    }

    [HttpPatch("{id:guid}/seats/{seatId:guid}/deactivate")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<object>>> DeactivateSeat(
        Guid id,
        Guid seatId,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new DeactivateSeatCommand(id, seatId), cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Seat deactivated."));
    }

    [HttpDelete("{id:guid}/seats/{seatId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<object>>> RemoveSeat(
        Guid id,
        Guid seatId,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new RemoveSeatCommand(id, seatId), cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Seat removed."));
    }
}
