using BusBooking.Application.Buses.DTOs;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Domain.Entities;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = BusBooking.Application.Common.Exceptions.ValidationException;

namespace BusBooking.Application.Buses.Commands.CreateBus;

public sealed class CreateBusCommandHandler : IRequestHandler<CreateBusCommand, BusDto>
{
    private readonly IApplicationDbContext _context;

    public CreateBusCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BusDto> Handle(CreateBusCommand request, CancellationToken cancellationToken)
    {
        var bus = new Bus(request.RegistrationNumber, request.Description, request.BusType);

        var alreadyExists = await _context.Buses
            .AnyAsync(b => b.RegistrationNumber == bus.RegistrationNumber, cancellationToken);

        if (alreadyExists)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.RegistrationNumber), "Registration number is already in use.")
            ]);
        }

        _context.Buses.Add(bus);
        await _context.SaveChangesAsync(cancellationToken);

        return bus.ToDto();
    }
}
