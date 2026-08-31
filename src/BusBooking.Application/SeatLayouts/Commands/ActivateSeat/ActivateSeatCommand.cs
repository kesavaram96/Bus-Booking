using MediatR;

namespace BusBooking.Application.SeatLayouts.Commands.ActivateSeat;

public sealed record ActivateSeatCommand(Guid SeatLayoutId, Guid SeatId) : IRequest;
