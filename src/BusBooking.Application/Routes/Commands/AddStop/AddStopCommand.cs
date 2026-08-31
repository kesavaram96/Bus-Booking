using BusBooking.Application.Routes.DTOs;
using MediatR;

namespace BusBooking.Application.Routes.Commands.AddStop;

public sealed record AddStopCommand(
    Guid RouteId,
    string StopName,
    TimeSpan? ExpectedArrivalTime,
    TimeSpan? ExpectedDepartureTime,
    bool AllowPickup,
    bool AllowDropOff) : IRequest<RouteStopDto>;
