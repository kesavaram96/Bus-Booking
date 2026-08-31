using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Reports.DTOs;
using BusBooking.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Reports.Queries.GetSeatOccupancyReport;

public sealed class GetSeatOccupancyReportQueryHandler : IRequestHandler<GetSeatOccupancyReportQuery, IReadOnlyList<SeatOccupancyReportEntryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetSeatOccupancyReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SeatOccupancyReportEntryDto>> Handle(
        GetSeatOccupancyReportQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Trips.AsNoTracking().AsQueryable();

        if (request.FromDate.HasValue)
        {
            query = query.Where(t => t.TripDate >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(t => t.TripDate <= request.ToDate.Value);
        }

        if (request.RouteId.HasValue)
        {
            query = query.Where(t => t.RouteId == request.RouteId.Value);
        }

        if (request.TripId.HasValue)
        {
            query = query.Where(t => t.Id == request.TripId.Value);
        }

        var raw = await query
            .Select(t => new
            {
                t.Id,
                t.TripDate,
                RouteFrom = t.Route.From,
                RouteTo = t.Route.To,
                TotalSeats = _context.TripSeats.Count(ts => ts.TripId == t.Id),
                BookedSeats = _context.Bookings
                    .Where(b => b.TripId == t.Id && b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.Refunded)
                    .SelectMany(b => b.Passengers)
                    .Select(p => p.SeatId)
                    .Distinct()
                    .Count()
            })
            .OrderBy(x => x.TripDate)
            .ToListAsync(cancellationToken);

        return raw
            .Select(x => new SeatOccupancyReportEntryDto(
                x.Id,
                x.TripDate,
                x.RouteFrom,
                x.RouteTo,
                x.TotalSeats,
                x.BookedSeats,
                x.TotalSeats == 0 ? 0m : Math.Round(100m * x.BookedSeats / x.TotalSeats, 2)))
            .ToList();
    }
}
