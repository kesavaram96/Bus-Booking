using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Reports.DTOs;
using BusBooking.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Reports.Queries.GetRevenueReport;

public sealed class GetRevenueReportQueryHandler : IRequestHandler<GetRevenueReportQuery, IReadOnlyList<RevenueReportEntryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetRevenueReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<RevenueReportEntryDto>> Handle(GetRevenueReportQuery request, CancellationToken cancellationToken)
    {
        var query =
            from payment in _context.Payments.AsNoTracking()
            join booking in _context.Bookings.AsNoTracking() on payment.BookingId equals booking.Id
            join trip in _context.Trips.AsNoTracking() on booking.TripId equals trip.Id
            where payment.Status == PaymentStatus.Paid
            select new { payment, booking, trip };

        if (request.FromDate.HasValue)
        {
            var from = request.FromDate.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.payment.PaidAt >= from);
        }

        if (request.ToDate.HasValue)
        {
            var toExclusive = request.ToDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.payment.PaidAt < toExclusive);
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

        var grouped = await query
            .GroupBy(x => x.payment.PaidAt!.Value.Date)
            .Select(g => new RevenueReportEntryDto(
                DateOnly.FromDateTime(g.Key),
                g.Count(),
                g.Sum(x => x.payment.Amount)))
            .ToListAsync(cancellationToken);

        return grouped.OrderBy(x => x.Date).ToList();
    }
}
