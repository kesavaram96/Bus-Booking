using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.SeatLayouts.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.SeatLayouts.Queries.GetSeatLayoutById;

public sealed class GetSeatLayoutByIdQueryHandler : IRequestHandler<GetSeatLayoutByIdQuery, SeatLayoutDto>
{
    private readonly IApplicationDbContext _context;

    public GetSeatLayoutByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SeatLayoutDto> Handle(GetSeatLayoutByIdQuery request, CancellationToken cancellationToken)
    {
        var layout = await _context.SeatLayouts
            .AsNoTracking()
            .Include(sl => sl.Seats)
            .FirstOrDefaultAsync(sl => sl.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("SeatLayout", request.Id);

        return layout.ToDto();
    }
}
