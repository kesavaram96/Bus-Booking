using BusBooking.Application.Routes.DTOs;
using MediatR;

namespace BusBooking.Application.Routes.Queries.GetActiveRoutes;

public sealed record GetActiveRoutesQuery : IRequest<IReadOnlyList<RouteSummaryDto>>;
