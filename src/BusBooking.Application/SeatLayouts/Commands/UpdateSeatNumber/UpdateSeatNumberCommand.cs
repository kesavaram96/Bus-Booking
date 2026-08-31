using BusBooking.Application.SeatLayouts.DTOs;
using MediatR;

namespace BusBooking.Application.SeatLayouts.Commands.UpdateSeatNumber;

public sealed record UpdateSeatNumberCommand(Guid SeatLayoutId, Guid SeatId, string SeatNumber) : IRequest<SeatDto>;
