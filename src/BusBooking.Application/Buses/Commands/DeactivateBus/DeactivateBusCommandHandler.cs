using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Buses.Commands.DeactivateBus;

public sealed class DeactivateBusCommandHandler : IRequestHandler<DeactivateBusCommand>
{
    private readonly IApplicationDbContext _context;

    public DeactivateBusCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeactivateBusCommand request, CancellationToken cancellationToken)
    {
        var bus = await _context.Buses.FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Bus", request.Id);

        bus.Deactivate();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
