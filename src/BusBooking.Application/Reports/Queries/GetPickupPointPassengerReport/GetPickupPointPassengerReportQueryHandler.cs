using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Reports.Common;
using BusBooking.Application.Reports.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Reports.Queries.GetPickupPointPassengerReport;

public sealed class GetPickupPointPassengerReportQueryHandler
    : IRequestHandler<GetPickupPointPassengerReportQuery, IReadOnlyList<PassengerReportEntryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPickupPointPassengerReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PassengerReportEntryDto>> Handle(
        GetPickupPointPassengerReportQuery request,
        CancellationToken cancellationToken)
    {
        var query = PassengerReportQueryHelper.BuildQuery(
            _context, request.FromDate, request.ToDate, request.RouteId, request.TripId, request.Status);

        return await query
            .OrderBy(p => p.PickupStopName)
            .ThenBy(p => p.TripDate)
            .ThenBy(p => p.SeatNumber)
            .ToListAsync(cancellationToken);
    }
}
