using BusBooking.API.Extensions;
using BusBooking.Application.Buses.Commands.ActivateBus;
using BusBooking.Application.Buses.Commands.AssignSeatLayout;
using BusBooking.Application.Buses.Commands.CreateBus;
using BusBooking.Application.Buses.Commands.DeactivateBus;
using BusBooking.Application.Buses.Commands.UpdateBus;
using BusBooking.Application.Buses.DTOs;
using BusBooking.Application.Buses.Queries.GetBuses;
using BusBooking.Application.Buses.Queries.GetBusById;
using BusBooking.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.API.Controllers;

[ApiController]
[Route("api/buses")]
[Authorize]
public class BusesController : ControllerBase
{
    private readonly ISender _sender;

    public BusesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.RequireBookingStaff)]
    public async Task<ActionResult<ApiResponse<PaginatedList<BusDto>>>> GetBuses(
        [FromQuery] GetBusesQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(ApiResponse<PaginatedList<BusDto>>.SuccessResponse(result));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireBookingStaff)]
    public async Task<ActionResult<ApiResponse<BusDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetBusByIdQuery(id), cancellationToken);
        return Ok(ApiResponse<BusDto>.SuccessResponse(result));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<BusDto>>> Create(CreateBusCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            ApiResponse<BusDto>.SuccessResponse(result, "Bus created."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<BusDto>>> Update(
        Guid id,
        UpdateBusCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command with { Id = id }, cancellationToken);
        return Ok(ApiResponse<BusDto>.SuccessResponse(result, "Bus updated."));
    }

    [HttpPatch("{id:guid}/activate")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<object>>> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new ActivateBusCommand(id), cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Bus activated."));
    }

    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<object>>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeactivateBusCommand(id), cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Bus deactivated."));
    }

    [HttpPatch("{id:guid}/seat-layout")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<BusDto>>> AssignSeatLayout(
        Guid id,
        AssignSeatLayoutRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new AssignSeatLayoutCommand(id, request.SeatLayoutId), cancellationToken);
        return Ok(ApiResponse<BusDto>.SuccessResponse(result, "Seat layout assigned."));
    }
}
