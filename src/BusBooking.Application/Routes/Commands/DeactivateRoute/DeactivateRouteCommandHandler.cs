using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Routes.Commands.DeactivateRoute;

public sealed class DeactivateRouteCommandHandler : IRequestHandler<DeactivateRouteCommand>
{
    private readonly IApplicationDbContext _context;

    public DeactivateRouteCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeactivateRouteCommand request, CancellationToken cancellationToken)
    {
        var route = await _context.Routes.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Route", request.Id);

        route.Deactivate();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
