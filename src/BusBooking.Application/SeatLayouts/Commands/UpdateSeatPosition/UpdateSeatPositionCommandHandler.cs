using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.SeatLayouts.DTOs;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = BusBooking.Application.Common.Exceptions.ValidationException;

namespace BusBooking.Application.SeatLayouts.Commands.UpdateSeatPosition;

public sealed class UpdateSeatPositionCommandHandler : IRequestHandler<UpdateSeatPositionCommand, SeatDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateSeatPositionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SeatDto> Handle(UpdateSeatPositionCommand request, CancellationToken cancellationToken)
    {
        var layout = await _context.SeatLayouts.FirstOrDefaultAsync(sl => sl.Id == request.SeatLayoutId, cancellationToken)
            ?? throw new NotFoundException("SeatLayout", request.SeatLayoutId);

        var seat = await _context.Seats.FirstOrDefaultAsync(
            s => s.Id == request.SeatId && s.SeatLayoutId == request.SeatLayoutId,
            cancellationToken)
            ?? throw new NotFoundException("Seat", request.SeatId);

        if (request.Row >= layout.Rows || request.Column >= layout.Columns)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.Row), "Seat position is outside the layout's declared rows/columns.")
            ]);
        }

        var positionTaken = await _context.Seats.AnyAsync(
            s => s.SeatLayoutId == request.SeatLayoutId
                 && s.Id != request.SeatId
                 && s.Row == request.Row
                 && s.Column == request.Column,
            cancellationToken);

        if (positionTaken)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.Row), "Another seat already occupies this row/column.")
            ]);
        }

        seat.UpdatePosition(request.Row, request.Column, request.PositionType);

        await _context.SaveChangesAsync(cancellationToken);

        return seat.ToDto();
    }
}
