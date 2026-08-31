using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Reports.Common;
using BusBooking.Application.Reports.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Reports.Queries.GetTripPassengerReport;

public sealed class GetTripPassengerReportQueryHandler : IRequestHandler<GetTripPassengerReportQuery, IReadOnlyList<PassengerReportEntryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetTripPassengerReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PassengerReportEntryDto>> Handle(
        GetTripPassengerReportQuery request,
        CancellationToken cancellationToken)
    {
        var query = PassengerReportQueryHelper.BuildQuery(
            _context, request.FromDate, request.ToDate, request.RouteId, request.TripId, request.Status);

        return await query
            .OrderBy(p => p.TripDate)
            .ThenBy(p => p.SeatNumber)
            .ToListAsync(cancellationToken);
    }
}
