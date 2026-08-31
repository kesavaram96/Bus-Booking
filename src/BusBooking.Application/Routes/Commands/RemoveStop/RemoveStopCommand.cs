using MediatR;

namespace BusBooking.Application.Routes.Commands.RemoveStop;

public sealed record RemoveStopCommand(Guid RouteId, Guid StopId) : IRequest;
