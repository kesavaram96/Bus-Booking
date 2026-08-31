using MediatR;

namespace BusBooking.Application.Trips.Commands.UnlockSeat;

public sealed record UnlockSeatCommand(Guid TripId, Guid TripSeatId, string LockId) : IRequest;
