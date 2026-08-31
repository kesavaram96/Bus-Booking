using BusBooking.Application.Routes.DTOs;
using MediatR;

namespace BusBooking.Application.Routes.Commands.ReorderStops;

public sealed record ReorderStopsCommand(Guid RouteId, IReadOnlyList<Guid> OrderedStopIds) : IRequest<RouteDto>;
