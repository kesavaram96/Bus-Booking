using BusBooking.Application.Buses.DTOs;
using MediatR;

namespace BusBooking.Application.Buses.Commands.AssignSeatLayout;

public sealed record AssignSeatLayoutCommand(Guid BusId, Guid SeatLayoutId) : IRequest<BusDto>;
