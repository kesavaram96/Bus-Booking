using BusBooking.Application.Common.Models;
using BusBooking.Application.SeatLayouts.DTOs;
using MediatR;

namespace BusBooking.Application.SeatLayouts.Queries.GetSeatLayouts;

public sealed record GetSeatLayoutsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    string? SortBy = null,
    bool SortDescending = false) : IRequest<PaginatedList<SeatLayoutSummaryDto>>;
