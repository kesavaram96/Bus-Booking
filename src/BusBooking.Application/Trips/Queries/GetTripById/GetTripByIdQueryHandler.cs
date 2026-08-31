using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Trips.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Trips.Queries.GetTripById;

public sealed class GetTripByIdQueryHandler : IRequestHandler<GetTripByIdQuery, TripDto>
{
    private readonly IApplicationDbContext _context;

    public GetTripByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TripDto> Handle(GetTripByIdQuery request, CancellationToken cancellationToken)
    {
        var trip = await _context.Trips
            .AsNoTracking()
            .Include(t => t.Route)
            .Include(t => t.Bus)
            .Include(t => t.Driver)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Trip", request.Id);

        return trip.ToDto();
    }
}
