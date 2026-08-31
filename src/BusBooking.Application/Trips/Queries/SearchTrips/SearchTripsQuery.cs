using BusBooking.Application.Common.Models;
using BusBooking.Application.Trips.DTOs;
using MediatR;

namespace BusBooking.Application.Trips.Queries.SearchTrips;

public sealed record SearchTripsQuery(
    string From,
    string To,
    DateOnly Date,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<TripSearchResultDto>>;
