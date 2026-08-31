using BusBooking.Application.Common.Models;
using BusBooking.Application.Trips.DTOs;
using BusBooking.Domain.Enums;
using MediatR;

namespace BusBooking.Application.Trips.Queries.GetTrips;

/// <summary>
/// One flexible, filterable query covers "upcoming trips" (FromDate = today), "trips by date"
/// (FromDate == ToDate) and "trips by route" (RouteId) rather than three separate endpoints.
/// </summary>
public sealed record GetTripsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    Guid? RouteId = null,
    Guid? BusId = null,
    TripStatus? Status = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    string? SortBy = null,
    bool SortDescending = false) : IRequest<PaginatedList<TripDto>>;
