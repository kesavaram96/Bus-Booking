using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Routes.DTOs;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = BusBooking.Application.Common.Exceptions.ValidationException;

namespace BusBooking.Application.Routes.Commands.ReorderStops;

public sealed class ReorderStopsCommandHandler : IRequestHandler<ReorderStopsCommand, RouteDto>
{
    private const int TemporaryOrderOffset = 100_000;

    private readonly IApplicationDbContext _context;

    public ReorderStopsCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RouteDto> Handle(ReorderStopsCommand request, CancellationToken cancellationToken)
    {
        var route = await _context.Routes
            .Include(r => r.Stops)
            .FirstOrDefaultAsync(r => r.Id == request.RouteId, cancellationToken)
            ?? throw new NotFoundException("Route", request.RouteId);

        var existingIds = route.Stops.Select(s => s.Id).ToHashSet();
        var requestedIds = request.OrderedStopIds.ToHashSet();

        if (!existingIds.SetEquals(requestedIds))
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(request.OrderedStopIds),
                    "The ordered stop list must include every stop in the route exactly once.")
            ]);
        }

        var stopsById = route.Stops.ToDictionary(s => s.Id);

        // Two-phase update: the unique (RouteId, StopOrder) index is checked per-statement, not
        // deferred to commit, so writing final orders directly can collide mid-transaction (e.g.
        // swapping stops 1 and 2). Move everything to a disjoint temporary range first.
        for (var i = 0; i < request.OrderedStopIds.Count; i++)
        {
            var stop = stopsById[request.OrderedStopIds[i]];
            stop.UpdateOrder(TemporaryOrderOffset + i + 1);
        }

        await _context.SaveChangesAsync(cancellationToken);

        for (var i = 0; i < request.OrderedStopIds.Count; i++)
        {
            var stop = stopsById[request.OrderedStopIds[i]];
            stop.UpdateOrder(i + 1);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return route.ToDto();
    }
}
