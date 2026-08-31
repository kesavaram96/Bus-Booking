using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Routes.DTOs;
using BusBooking.Domain.Entities;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = BusBooking.Application.Common.Exceptions.ValidationException;

namespace BusBooking.Application.Routes.Commands.AddStop;

public sealed class AddStopCommandHandler : IRequestHandler<AddStopCommand, RouteStopDto>
{
    private readonly IApplicationDbContext _context;

    public AddStopCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RouteStopDto> Handle(AddStopCommand request, CancellationToken cancellationToken)
    {
        var route = await _context.Routes
            .Include(r => r.Stops)
            .FirstOrDefaultAsync(r => r.Id == request.RouteId, cancellationToken)
            ?? throw new NotFoundException("Route", request.RouteId);

        var normalizedStopName = request.StopName.Trim();

        var duplicateStopName = route.Stops.Any(
            s => string.Equals(s.StopName, normalizedStopName, StringComparison.OrdinalIgnoreCase));

        if (duplicateStopName)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.StopName), "This stop already exists in the route.")
            ]);
        }

        var nextOrder = route.Stops.Count == 0 ? 1 : route.Stops.Max(s => s.StopOrder) + 1;

        var stop = new RouteStop(
            route.Id,
            request.StopName,
            nextOrder,
            request.ExpectedArrivalTime,
            request.ExpectedDepartureTime,
            request.AllowPickup,
            request.AllowDropOff);

        _context.RouteStops.Add(stop);
        await _context.SaveChangesAsync(cancellationToken);

        return stop.ToDto();
    }
}
