using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Routes.DTOs;
using BusBooking.Domain.Entities;
using MediatR;

namespace BusBooking.Application.Routes.Commands.CreateRoute;

public sealed class CreateRouteCommandHandler : IRequestHandler<CreateRouteCommand, RouteDto>
{
    private readonly IApplicationDbContext _context;

    public CreateRouteCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RouteDto> Handle(CreateRouteCommand request, CancellationToken cancellationToken)
    {
        var route = new Route(request.Name, request.From, request.To);

        _context.Routes.Add(route);
        await _context.SaveChangesAsync(cancellationToken);

        return route.ToDto();
    }
}
