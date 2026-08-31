using BusBooking.Application.SeatLayouts.DTOs;
using MediatR;

namespace BusBooking.Application.SeatLayouts.Commands.UpdateSeatLayout;

public sealed record UpdateSeatLayoutCommand(
    Guid Id,
    string Name,
    string? Description,
    int Rows,
    int Columns) : IRequest<SeatLayoutDto>;
