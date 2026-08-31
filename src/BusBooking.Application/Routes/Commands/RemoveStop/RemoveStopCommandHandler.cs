using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = BusBooking.Application.Common.Exceptions.ValidationException;

namespace BusBooking.Application.Routes.Commands.RemoveStop;

public sealed class RemoveStopCommandHandler : IRequestHandler<RemoveStopCommand>
{
    private readonly IApplicationDbContext _context;

    public RemoveStopCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(RemoveStopCommand request, CancellationToken cancellationToken)
    {
        var route = await _context.Routes
            .Include(r => r.Stops)
            .FirstOrDefaultAsync(r => r.Id == request.RouteId, cancellationToken)
            ?? throw new NotFoundException("Route", request.RouteId);

        var stop = route.Stops.FirstOrDefault(s => s.Id == request.StopId)
            ?? throw new NotFoundException("RouteStop", request.StopId);

        // An active route must keep satisfying "at least two stops"; a draft/inactive route
        // can be freely rebuilt (including down to zero stops) before it is ever activated.
        if (route.IsActive && route.Stops.Count <= 2)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.StopId), "An active route must have at least two stops.")
            ]);
        }

        _context.RouteStops.Remove(stop);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
