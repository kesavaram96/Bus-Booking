using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = BusBooking.Application.Common.Exceptions.ValidationException;

namespace BusBooking.Application.Routes.Commands.ActivateRoute;

public sealed class ActivateRouteCommandHandler : IRequestHandler<ActivateRouteCommand>
{
    private readonly IApplicationDbContext _context;

    public ActivateRouteCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(ActivateRouteCommand request, CancellationToken cancellationToken)
    {
        var route = await _context.Routes
            .Include(r => r.Stops)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Route", request.Id);

        if (route.Stops.Count < 2)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.Id), "A route must have at least two stops before it can be activated.")
            ]);
        }

        route.Activate();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
