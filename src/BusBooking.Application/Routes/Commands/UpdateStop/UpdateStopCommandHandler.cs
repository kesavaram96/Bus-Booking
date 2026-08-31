using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Routes.DTOs;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = BusBooking.Application.Common.Exceptions.ValidationException;

namespace BusBooking.Application.Routes.Commands.UpdateStop;

public sealed class UpdateStopCommandHandler : IRequestHandler<UpdateStopCommand, RouteStopDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateStopCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RouteStopDto> Handle(UpdateStopCommand request, CancellationToken cancellationToken)
    {
        var route = await _context.Routes
            .Include(r => r.Stops)
            .FirstOrDefaultAsync(r => r.Id == request.RouteId, cancellationToken)
            ?? throw new NotFoundException("Route", request.RouteId);

        var stop = route.Stops.FirstOrDefault(s => s.Id == request.StopId)
            ?? throw new NotFoundException("RouteStop", request.StopId);

        var normalizedStopName = request.StopName.Trim();

        var duplicateStopName = route.Stops.Any(
            s => s.Id != request.StopId
                 && string.Equals(s.StopName, normalizedStopName, StringComparison.OrdinalIgnoreCase));

        if (duplicateStopName)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.StopName), "This stop already exists in the route.")
            ]);
        }

        stop.UpdateDetails(
            request.StopName,
            request.ExpectedArrivalTime,
            request.ExpectedDepartureTime,
            request.AllowPickup,
            request.AllowDropOff);

        await _context.SaveChangesAsync(cancellationToken);

        return stop.ToDto();
    }
}
