using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Routes.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Routes.Queries.GetRouteById;

public sealed class GetRouteByIdQueryHandler : IRequestHandler<GetRouteByIdQuery, RouteDto>
{
    private readonly IApplicationDbContext _context;

    public GetRouteByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RouteDto> Handle(GetRouteByIdQuery request, CancellationToken cancellationToken)
    {
        var route = await _context.Routes
            .AsNoTracking()
            .Include(r => r.Stops)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Route", request.Id);

        return route.ToDto();
    }
}
