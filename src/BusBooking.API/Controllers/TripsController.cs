using BusBooking.API.Extensions;
using BusBooking.Application.Common.Models;
using BusBooking.Application.Trips.Commands.AssignBus;
using BusBooking.Application.Trips.Commands.AssignDriver;
using BusBooking.Application.Trips.Commands.BlockSeat;
using BusBooking.Application.Trips.Commands.CancelTrip;
using BusBooking.Application.Trips.Commands.CreateTrip;
using BusBooking.Application.Trips.Commands.LockSeat;
using BusBooking.Application.Trips.Commands.MarkBoarding;
using BusBooking.Application.Trips.Commands.MarkCompleted;
using BusBooking.Application.Trips.Commands.MarkDeparted;
using BusBooking.Application.Trips.Commands.RemoveDriver;
using BusBooking.Application.Trips.Commands.ScheduleTrip;
using BusBooking.Application.Trips.Commands.UnblockSeat;
using BusBooking.Application.Trips.Commands.UnlockSeat;
using BusBooking.Application.Trips.Commands.UpdateTrip;
using BusBooking.Application.Trips.DTOs;
using BusBooking.Application.Trips.Queries.GetPassengerManifest;
using BusBooking.Application.Trips.Queries.GetTripById;
using BusBooking.Application.Trips.Queries.GetTripSeatMap;
using BusBooking.Application.Trips.Queries.GetTripSeats;
using BusBooking.Application.Trips.Queries.GetTrips;
using BusBooking.Application.Trips.Queries.SearchTrips;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.API.Controllers;

[ApiController]
[Route("api/trips")]
[Authorize]
public class TripsController : ControllerBase
{
    private readonly ISender _sender;

    public TripsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.RequireBookingStaff)]
    public async Task<ActionResult<ApiResponse<PaginatedList<TripDto>>>> GetTrips(
        [FromQuery] GetTripsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(ApiResponse<PaginatedList<TripDto>>.SuccessResponse(result));
    }

    /// <summary>
    /// Public customer-facing trip search — no account required (guests must be able to
    /// search before deciding to book). Returns a deliberately restricted shape; see
    /// TripSearchResultDto.
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PaginatedList<TripSearchResultDto>>>> Search(
        [FromQuery] SearchTripsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(ApiResponse<PaginatedList<TripSearchResultDto>>.SuccessResponse(result));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireBookingStaff)]
    public async Task<ActionResult<ApiResponse<TripDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTripByIdQuery(id), cancellationToken);
        return Ok(ApiResponse<TripDto>.SuccessResponse(result));
    }

    /// <summary>
    /// Public seat map — no account required, same reasoning as /search. No passenger or
    /// booking information, only what's needed to render seats and pick an available one.
    /// </summary>
    [HttpGet("{id:guid}/seat-map")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<SeatMapDto>>> GetSeatMap(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTripSeatMapQuery(id), cancellationToken);
        return Ok(ApiResponse<SeatMapDto>.SuccessResponse(result));
    }

    [HttpGet("{id:guid}/seats")]
    [Authorize(Policy = AuthorizationPolicies.RequireBookingStaff)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TripSeatDto>>>> GetSeats(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTripSeatsQuery(id), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TripSeatDto>>.SuccessResponse(result));
    }

    /// <summary>
    /// Backs an A4-printable passenger register, and later PDF/Excel export from the same
    /// data — so it deliberately returns the full (filtered/sorted) list rather than a page.
    /// Staff-only: full passenger PII (phone number, NIC-adjacent search) for an entire trip.
    /// </summary>
    [HttpGet("{id:guid}/passenger-manifest")]
    [Authorize(Policy = AuthorizationPolicies.RequireBookingStaff)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PassengerManifestEntryDto>>>> GetPassengerManifest(
        Guid id,
        [FromQuery] GetPassengerManifestQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query with { TripId = id }, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PassengerManifestEntryDto>>.SuccessResponse(result));
    }

    [HttpPatch("{id:guid}/seats/{tripSeatId:guid}/block")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<TripSeatDto>>> BlockSeat(
        Guid id,
        Guid tripSeatId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new BlockSeatCommand(id, tripSeatId), cancellationToken);
        return Ok(ApiResponse<TripSeatDto>.SuccessResponse(result, "Seat blocked."));
    }

    [HttpPatch("{id:guid}/seats/{tripSeatId:guid}/unblock")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<TripSeatDto>>> UnblockSeat(
        Guid id,
        Guid tripSeatId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UnblockSeatCommand(id, tripSeatId), cancellationToken);
        return Ok(ApiResponse<TripSeatDto>.SuccessResponse(result, "Seat unblocked."));
    }

    /// <summary>
    /// Public — no account required, matching /search and /seat-map (a guest must be able to
    /// hold a seat while filling in the rest of the booking flow before ever creating one).
    /// Redis atomically arbitrates concurrent attempts on the same seat.
    /// </summary>
    [HttpPost("{id:guid}/seats/{tripSeatId:guid}/lock")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<SeatLockDto>>> LockSeat(
        Guid id,
        Guid tripSeatId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new LockSeatCommand(id, tripSeatId), cancellationToken);
        return Ok(ApiResponse<SeatLockDto>.SuccessResponse(result, "Seat locked."));
    }

    [HttpPost("{id:guid}/seats/{tripSeatId:guid}/unlock")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> UnlockSeat(
        Guid id,
        Guid tripSeatId,
        UnlockSeatRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new UnlockSeatCommand(id, tripSeatId, request.LockId), cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Seat unlocked."));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<TripDto>>> Create(
        CreateTripCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            ApiResponse<TripDto>.SuccessResponse(result, "Trip created."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<TripDto>>> Update(
        Guid id,
        UpdateTripCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command with { Id = id }, cancellationToken);
        return Ok(ApiResponse<TripDto>.SuccessResponse(result, "Trip updated."));
    }

    [HttpPatch("{id:guid}/bus")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<TripDto>>> AssignBus(
        Guid id,
        AssignBusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new AssignBusCommand(id, request.BusId), cancellationToken);
        return Ok(ApiResponse<TripDto>.SuccessResponse(result, "Bus assigned."));
    }

    [HttpPatch("{id:guid}/driver")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<TripDto>>> AssignDriver(
        Guid id,
        AssignDriverRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new AssignDriverCommand(id, request.DriverId), cancellationToken);
        return Ok(ApiResponse<TripDto>.SuccessResponse(result, "Driver assigned."));
    }

    [HttpDelete("{id:guid}/driver")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<TripDto>>> RemoveDriver(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RemoveDriverCommand(id), cancellationToken);
        return Ok(ApiResponse<TripDto>.SuccessResponse(result, "Driver removed."));
    }

    [HttpPatch("{id:guid}/schedule")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<object>>> Schedule(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new ScheduleTripCommand(id), cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Trip scheduled."));
    }

    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<object>>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new CancelTripCommand(id), cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Trip cancelled."));
    }

    [HttpPatch("{id:guid}/boarding")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<object>>> MarkBoarding(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new MarkBoardingCommand(id), cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Trip marked as boarding."));
    }

    [HttpPatch("{id:guid}/departed")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<object>>> MarkDeparted(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new MarkDepartedCommand(id), cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Trip marked as departed."));
    }

    [HttpPatch("{id:guid}/completed")]
    [Authorize(Policy = AuthorizationPolicies.RequireOperationsStaff)]
    public async Task<ActionResult<ApiResponse<object>>> MarkCompleted(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new MarkCompletedCommand(id), cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Trip marked as completed."));
    }
}
