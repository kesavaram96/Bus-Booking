using BusBooking.Application.Reports.DTOs;
using BusBooking.Domain.Enums;
using MediatR;

namespace BusBooking.Application.Reports.Queries.GetCustomerBookingHistory;

/// <summary>FromDate/ToDate filter on Booking.CreatedAt.</summary>
public sealed record GetCustomerBookingHistoryQuery(
    Guid CustomerId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    Guid? RouteId,
    Guid? TripId,
    BookingStatus? Status) : IRequest<IReadOnlyList<CustomerBookingHistoryEntryDto>>;
