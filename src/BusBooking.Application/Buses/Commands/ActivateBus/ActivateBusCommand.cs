using MediatR;

namespace BusBooking.Application.Buses.Commands.ActivateBus;

public sealed record ActivateBusCommand(Guid Id) : IRequest;
