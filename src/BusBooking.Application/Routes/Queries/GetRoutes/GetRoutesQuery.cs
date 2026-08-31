using BusBooking.Application.Common.Models;
using BusBooking.Application.Routes.DTOs;
using MediatR;

namespace BusBooking.Application.Routes.Queries.GetRoutes;

public sealed record GetRoutesQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    bool? IsActive = null,
    string? SortBy = null,
    bool SortDescending = false) : IRequest<PaginatedList<RouteSummaryDto>>;
