using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.SeatLayouts.DTOs;
using BusBooking.Domain.Entities;
using MediatR;

namespace BusBooking.Application.SeatLayouts.Commands.CreateSeatLayout;

public sealed class CreateSeatLayoutCommandHandler : IRequestHandler<CreateSeatLayoutCommand, SeatLayoutDto>
{
    private readonly IApplicationDbContext _context;

    public CreateSeatLayoutCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SeatLayoutDto> Handle(CreateSeatLayoutCommand request, CancellationToken cancellationToken)
    {
        var layout = new SeatLayout(request.Name, request.Description, request.Rows, request.Columns);

        _context.SeatLayouts.Add(layout);
        await _context.SaveChangesAsync(cancellationToken);

        return layout.ToDto();
    }
}
