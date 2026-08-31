using BusBooking.Application.SeatLayouts.DTOs;
using MediatR;

namespace BusBooking.Application.SeatLayouts.Commands.CreateSeatLayout;

public sealed record CreateSeatLayoutCommand(
    string Name,
    string? Description,
    int Rows,
    int Columns) : IRequest<SeatLayoutDto>;
