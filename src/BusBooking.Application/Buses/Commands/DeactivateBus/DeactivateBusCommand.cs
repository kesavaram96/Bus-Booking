using MediatR;

namespace BusBooking.Application.Buses.Commands.DeactivateBus;

public sealed record DeactivateBusCommand(Guid Id) : IRequest;
