using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Reports.DTOs;
using BusBooking.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Reports.Queries.GetCancellationReport;

public sealed class GetCancellationReportQueryHandler : IRequestHandler<GetCancellationReportQuery, IReadOnlyList<CancellationReportEntryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCancellationReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CancellationReportEntryDto>> Handle(
        GetCancellationReportQuery request,
        CancellationToken cancellationToken)
    {
        var query =
            from booking in _context.Bookings.AsNoTracking()
            join trip in _context.Trips.AsNoTracking() on booking.TripId equals trip.Id
            where booking.CancelledAt != null
            select new { booking, trip };

        query = request.Status.HasValue
            ? query.Where(x => x.booking.Status == request.Status.Value)
            : query.Where(x => x.booking.Status == BookingStatus.Cancelled || x.booking.Status == BookingStatus.Refunded);

        if (request.FromDate.HasValue)
        {
            var from = request.FromDate.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.booking.CancelledAt >= from);
        }

        if (request.ToDate.HasValue)
        {
            var toExclusive = request.ToDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.booking.CancelledAt < toExclusive);
        }

        if (request.RouteId.HasValue)
        {
            query = query.Where(x => x.trip.RouteId == request.RouteId.Value);
        }

        if (request.TripId.HasValue)
        {
            query = query.Where(x => x.trip.Id == request.TripId.Value);
        }

        return await query
            .OrderByDescending(x => x.booking.CancelledAt)
            .Select(x => new CancellationReportEntryDto(
                x.booking.Id,
                x.booking.BookingNumber,
                x.booking.CancelledAt!.Value,
                x.booking.CancellationReason,
                x.booking.CancelledBy,
                x.booking.Status,
                x.booking.TotalAmount,
                x.trip.Id,
                x.trip.TripDate))
            .ToListAsync(cancellationToken);
    }
}
