using BusBooking.Application.Buses.DTOs;
using BusBooking.Application.Common.Models;
using BusBooking.Domain.Enums;
using MediatR;

namespace BusBooking.Application.Buses.Queries.GetBuses;

public sealed record GetBusesQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    BusType? BusType = null,
    BusStatus? Status = null,
    string? SortBy = null,
    bool SortDescending = false) : IRequest<PaginatedList<BusDto>>;
