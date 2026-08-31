using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.SeatLayouts.DTOs;
using BusBooking.Domain.Entities;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = BusBooking.Application.Common.Exceptions.ValidationException;

namespace BusBooking.Application.SeatLayouts.Commands.AddSeat;

public sealed class AddSeatCommandHandler : IRequestHandler<AddSeatCommand, SeatDto>
{
    private readonly IApplicationDbContext _context;

    public AddSeatCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SeatDto> Handle(AddSeatCommand request, CancellationToken cancellationToken)
    {
        var layout = await _context.SeatLayouts.FirstOrDefaultAsync(sl => sl.Id == request.SeatLayoutId, cancellationToken)
            ?? throw new NotFoundException("SeatLayout", request.SeatLayoutId);

        if (request.Row >= layout.Rows || request.Column >= layout.Columns)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.Row), "Seat position is outside the layout's declared rows/columns.")
            ]);
        }

        var normalizedSeatNumber = request.SeatNumber.Trim();

        var seatNumberTaken = await _context.Seats.AnyAsync(
            s => s.SeatLayoutId == request.SeatLayoutId && s.SeatNumber == normalizedSeatNumber,
            cancellationToken);

        if (seatNumberTaken)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.SeatNumber), "Seat number is already used within this layout.")
            ]);
        }

        var positionTaken = await _context.Seats.AnyAsync(
            s => s.SeatLayoutId == request.SeatLayoutId && s.Row == request.Row && s.Column == request.Column,
            cancellationToken);

        if (positionTaken)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.Row), "Another seat already occupies this row/column.")
            ]);
        }

        var seat = new Seat(request.SeatLayoutId, request.SeatNumber, request.Row, request.Column, request.PositionType);

        _context.Seats.Add(seat);
        await _context.SaveChangesAsync(cancellationToken);

        return seat.ToDto();
    }
}
