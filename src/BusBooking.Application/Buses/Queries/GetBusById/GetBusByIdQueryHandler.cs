using BusBooking.Application.Buses.DTOs;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Buses.Queries.GetBusById;

public sealed class GetBusByIdQueryHandler : IRequestHandler<GetBusByIdQuery, BusDto>
{
    private readonly IApplicationDbContext _context;

    public GetBusByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BusDto> Handle(GetBusByIdQuery request, CancellationToken cancellationToken)
    {
        var bus = await _context.Buses
            .AsNoTracking()
            .Include(b => b.SeatLayout)
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Bus", request.Id);

        return bus.ToDto();
    }
}
