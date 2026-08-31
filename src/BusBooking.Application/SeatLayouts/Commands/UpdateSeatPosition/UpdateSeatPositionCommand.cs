using BusBooking.Application.SeatLayouts.DTOs;
using BusBooking.Domain.Enums;
using MediatR;

namespace BusBooking.Application.SeatLayouts.Commands.UpdateSeatPosition;

public sealed record UpdateSeatPositionCommand(
    Guid SeatLayoutId,
    Guid SeatId,
    int Row,
    int Column,
    SeatPositionType PositionType) : IRequest<SeatDto>;
