using BusBooking.Application.Routes.DTOs;
using MediatR;

namespace BusBooking.Application.Routes.Queries.GetRouteById;

public sealed record GetRouteByIdQuery(Guid Id) : IRequest<RouteDto>;
