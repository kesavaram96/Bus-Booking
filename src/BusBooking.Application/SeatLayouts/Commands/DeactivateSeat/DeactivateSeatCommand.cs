using MediatR;

namespace BusBooking.Application.SeatLayouts.Commands.DeactivateSeat;

public sealed record DeactivateSeatCommand(Guid SeatLayoutId, Guid SeatId) : IRequest;
