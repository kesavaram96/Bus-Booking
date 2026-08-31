using BusBooking.Application.Buses.DTOs;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Buses.Commands.AssignSeatLayout;

public sealed class AssignSeatLayoutCommandHandler : IRequestHandler<AssignSeatLayoutCommand, BusDto>
{
    private readonly IApplicationDbContext _context;

    public AssignSeatLayoutCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BusDto> Handle(AssignSeatLayoutCommand request, CancellationToken cancellationToken)
    {
        var bus = await _context.Buses.FirstOrDefaultAsync(b => b.Id == request.BusId, cancellationToken)
            ?? throw new NotFoundException("Bus", request.BusId);

        var seatLayout = await _context.SeatLayouts.FirstOrDefaultAsync(sl => sl.Id == request.SeatLayoutId, cancellationToken)
            ?? throw new NotFoundException("SeatLayout", request.SeatLayoutId);

        bus.AssignSeatLayout(seatLayout.Id);

        await _context.SaveChangesAsync(cancellationToken);

        return bus.ToDto(seatLayout.Name);
    }
}
