using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.SeatLayouts.DTOs;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = BusBooking.Application.Common.Exceptions.ValidationException;

namespace BusBooking.Application.SeatLayouts.Commands.UpdateSeatLayout;

public sealed class UpdateSeatLayoutCommandHandler : IRequestHandler<UpdateSeatLayoutCommand, SeatLayoutDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateSeatLayoutCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SeatLayoutDto> Handle(UpdateSeatLayoutCommand request, CancellationToken cancellationToken)
    {
        var layout = await _context.SeatLayouts
            .Include(sl => sl.Seats)
            .FirstOrDefaultAsync(sl => sl.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("SeatLayout", request.Id);

        var outOfBounds = layout.Seats.Any(s => s.Row >= request.Rows || s.Column >= request.Columns);
        if (outOfBounds)
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    nameof(request.Rows),
                    "Layout dimensions cannot be smaller than the position of an existing seat. Remove or reposition those seats first.")
            ]);
        }

        layout.UpdateDetails(request.Name, request.Description, request.Rows, request.Columns);

        await _context.SaveChangesAsync(cancellationToken);

        return layout.ToDto();
    }
}
