using BusBooking.Application.Bookings.DTOs;
using BusBooking.Application.Common.Models;
using BusBooking.Domain.Enums;
using MediatR;

namespace BusBooking.Application.Bookings.Queries.GetBookings;

public sealed record GetBookingsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    Guid? TripId = null,
    Guid? CustomerId = null,
    BookingStatus? Status = null,
    string? SortBy = null,
    bool SortDescending = false) : IRequest<PaginatedList<BookingDto>>;
