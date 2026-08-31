using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Trips.Commands.MarkBoarding;

public sealed class MarkBoardingCommandHandler : IRequestHandler<MarkBoardingCommand>
{
    private readonly IApplicationDbContext _context;

    public MarkBoardingCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(MarkBoardingCommand request, CancellationToken cancellationToken)
    {
        var trip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, cancellationToken)
            ?? throw new NotFoundException("Trip", request.TripId);

        trip.MarkBoarding();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
