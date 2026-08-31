using BusBooking.API.Extensions;
using BusBooking.Application.Common.Models;
using BusBooking.Application.Routes.Commands.ActivateRoute;
using BusBooking.Application.Routes.Commands.AddStop;
using BusBooking.Application.Routes.Commands.CreateRoute;
using BusBooking.Application.Routes.Commands.DeactivateRoute;
using BusBooking.Application.Routes.Commands.RemoveStop;
using BusBooking.Application.Routes.Commands.ReorderStops;
using BusBooking.Application.Routes.Commands.UpdateRoute;
using BusBooking.Application.Routes.Commands.UpdateStop;
using BusBooking.Application.Routes.DTOs;
using BusBooking.Application.Routes.Queries.GetActiveRoutes;
using BusBooking.Application.Routes.Queries.GetRouteById;
using BusBooking.Application.Routes.Queries.GetRoutes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.API.Controllers;

[ApiController]
[Route("api/routes")]
[Authorize]
public class RoutesController : ControllerBase
{
    private readonly ISender _sender;

    public RoutesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.RequireBookingStaff)]
    public async Task<ActionResult<ApiResponse<PaginatedList<RouteSummaryDto>>>> GetRoutes(
        [FromQuery] GetRoutesQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(ApiResponse<PaginatedList<RouteSummaryDto>>.SuccessResponse(result));
    }

    [HttpGet("active")]
    [Authorize(Policy = AuthorizationPolicies.RequireBookingStaff)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RouteSummaryDto>>>> GetActiveRoutes(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetActiveRoutesQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RouteSummaryDto>>.SuccessResponse(result));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireBookingStaff)]
    public async Task<ActionResult<ApiResponse<RouteDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetRouteByIdQuery(id), cancellationToken);
        return Ok(ApiResponse<RouteDto>.SuccessResponse(result));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<RouteDto>>> Create(
        CreateRouteCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            ApiResponse<RouteDto>.SuccessResponse(result, "Route created."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<RouteDto>>> Update(
        Guid id,
        UpdateRouteCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command with { Id = id }, cancellationToken);
        return Ok(ApiResponse<RouteDto>.SuccessResponse(result, "Route updated."));
    }

    [HttpPatch("{id:guid}/activate")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<object>>> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new ActivateRouteCommand(id), cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Route activated."));
    }

    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<object>>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeactivateRouteCommand(id), cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Route deactivated."));
    }

    [HttpPost("{id:guid}/stops")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<RouteStopDto>>> AddStop(
        Guid id,
        AddStopCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command with { RouteId = id }, cancellationToken);
        return Ok(ApiResponse<RouteStopDto>.SuccessResponse(result, "Stop added."));
    }

    [HttpPut("{id:guid}/stops/{stopId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<RouteStopDto>>> UpdateStop(
        Guid id,
        Guid stopId,
        UpdateStopCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command with { RouteId = id, StopId = stopId }, cancellationToken);
        return Ok(ApiResponse<RouteStopDto>.SuccessResponse(result, "Stop updated."));
    }

    [HttpDelete("{id:guid}/stops/{stopId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<object>>> RemoveStop(
        Guid id,
        Guid stopId,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new RemoveStopCommand(id, stopId), cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Stop removed."));
    }

    [HttpPut("{id:guid}/stops/reorder")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<RouteDto>>> ReorderStops(
        Guid id,
        ReorderStopsCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command with { RouteId = id }, cancellationToken);
        return Ok(ApiResponse<RouteDto>.SuccessResponse(result, "Stops reordered."));
    }
}
