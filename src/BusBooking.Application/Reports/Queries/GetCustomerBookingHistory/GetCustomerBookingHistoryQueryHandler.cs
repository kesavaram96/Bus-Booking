using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Reports.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Reports.Queries.GetCustomerBookingHistory;

public sealed class GetCustomerBookingHistoryQueryHandler
    : IRequestHandler<GetCustomerBookingHistoryQuery, IReadOnlyList<CustomerBookingHistoryEntryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCustomerBookingHistoryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CustomerBookingHistoryEntryDto>> Handle(
        GetCustomerBookingHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var query =
            from booking in _context.Bookings.AsNoTracking()
            join trip in _context.Trips.AsNoTracking() on booking.TripId equals trip.Id
            where booking.CustomerId == request.CustomerId
            select new { booking, trip };

        if (request.FromDate.HasValue)
        {
            var from = request.FromDate.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.booking.CreatedAt >= from);
        }

        if (request.ToDate.HasValue)
        {
            var toExclusive = request.ToDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.booking.CreatedAt < toExclusive);
        }

        if (request.RouteId.HasValue)
        {
            query = query.Where(x => x.trip.RouteId == request.RouteId.Value);
        }

        if (request.TripId.HasValue)
        {
            query = query.Where(x => x.trip.Id == request.TripId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.booking.Status == request.Status.Value);
        }

        return await query
            .OrderByDescending(x => x.booking.CreatedAt)
            .Select(x => new CustomerBookingHistoryEntryDto(
                x.booking.Id,
                x.booking.BookingNumber,
                x.booking.CreatedAt,
                x.booking.Status,
                x.booking.TotalAmount,
                x.trip.Id,
                x.trip.TripDate,
                x.trip.Route.From,
                x.trip.Route.To))
            .ToListAsync(cancellationToken);
    }
}
