using BusBooking.Application.SeatLayouts.DTOs;
using BusBooking.Domain.Enums;
using MediatR;

namespace BusBooking.Application.SeatLayouts.Commands.AddSeat;

public sealed record AddSeatCommand(
    Guid SeatLayoutId,
    string SeatNumber,
    int Row,
    int Column,
    SeatPositionType PositionType) : IRequest<SeatDto>;
