using MediatR;

namespace BusBooking.Application.SeatLayouts.Commands.RemoveSeat;

public sealed record RemoveSeatCommand(Guid SeatLayoutId, Guid SeatId) : IRequest;
