using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.SeatLayouts.DTOs;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = BusBooking.Application.Common.Exceptions.ValidationException;

namespace BusBooking.Application.SeatLayouts.Commands.UpdateSeatNumber;

public sealed class UpdateSeatNumberCommandHandler : IRequestHandler<UpdateSeatNumberCommand, SeatDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateSeatNumberCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SeatDto> Handle(UpdateSeatNumberCommand request, CancellationToken cancellationToken)
    {
        var seat = await _context.Seats.FirstOrDefaultAsync(
            s => s.Id == request.SeatId && s.SeatLayoutId == request.SeatLayoutId,
            cancellationToken)
            ?? throw new NotFoundException("Seat", request.SeatId);

        var normalizedSeatNumber = request.SeatNumber.Trim();

        var seatNumberTaken = await _context.Seats.AnyAsync(
            s => s.SeatLayoutId == request.SeatLayoutId
                 && s.Id != request.SeatId
                 && s.SeatNumber == normalizedSeatNumber,
            cancellationToken);

        if (seatNumberTaken)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.SeatNumber), "Seat number is already used within this layout.")
            ]);
        }

        seat.UpdateSeatNumber(request.SeatNumber);

        await _context.SaveChangesAsync(cancellationToken);

        return seat.ToDto();
    }
}
