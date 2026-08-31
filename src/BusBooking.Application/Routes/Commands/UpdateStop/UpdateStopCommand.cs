using BusBooking.Application.Routes.DTOs;
using MediatR;

namespace BusBooking.Application.Routes.Commands.UpdateStop;

public sealed record UpdateStopCommand(
    Guid RouteId,
    Guid StopId,
    string StopName,
    TimeSpan? ExpectedArrivalTime,
    TimeSpan? ExpectedDepartureTime,
    bool AllowPickup,
    bool AllowDropOff) : IRequest<RouteStopDto>;
