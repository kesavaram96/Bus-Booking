using BusBooking.Application.Trips.DTOs;
using MediatR;

namespace BusBooking.Application.Trips.Commands.LockSeat;

public sealed record LockSeatCommand(Guid TripId, Guid TripSeatId) : IRequest<SeatLockDto>;
