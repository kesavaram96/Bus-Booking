using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Routes.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Routes.Commands.UpdateRoute;

public sealed class UpdateRouteCommandHandler : IRequestHandler<UpdateRouteCommand, RouteDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateRouteCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RouteDto> Handle(UpdateRouteCommand request, CancellationToken cancellationToken)
    {
        var route = await _context.Routes
            .Include(r => r.Stops)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Route", request.Id);

        route.UpdateDetails(request.Name, request.From, request.To);

        await _context.SaveChangesAsync(cancellationToken);

        return route.ToDto();
    }
}
