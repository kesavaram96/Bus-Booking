using BusBooking.Application.Buses.DTOs;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = BusBooking.Application.Common.Exceptions.ValidationException;

namespace BusBooking.Application.Buses.Commands.UpdateBus;

public sealed class UpdateBusCommandHandler : IRequestHandler<UpdateBusCommand, BusDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateBusCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BusDto> Handle(UpdateBusCommand request, CancellationToken cancellationToken)
    {
        var bus = await _context.Buses
            .Include(b => b.SeatLayout)
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Bus", request.Id);

        var normalizedRegistrationNumber = request.RegistrationNumber.Trim().ToUpperInvariant();

        var registrationNumberTaken = await _context.Buses
            .AnyAsync(
                b => b.Id != request.Id && b.RegistrationNumber == normalizedRegistrationNumber,
                cancellationToken);

        if (registrationNumberTaken)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.RegistrationNumber), "Registration number is already in use.")
            ]);
        }

        bus.UpdateDetails(request.RegistrationNumber, request.Description, request.BusType);

        await _context.SaveChangesAsync(cancellationToken);

        return bus.ToDto();
    }
}
