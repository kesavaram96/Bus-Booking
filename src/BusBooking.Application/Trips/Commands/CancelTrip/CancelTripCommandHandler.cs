using BusBooking.Application.Bookings.Common;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Trips.Commands.CancelTrip;

/// <summary>
/// Cancelling a trip cascades to every Pending/Confirmed booking on it (system-triggered:
/// CancelledBy is null, distinct from a human-initiated cancellation) — added in Phase 17
/// alongside booking cancellation itself, since leaving those bookings untouched would mean
/// seats "sold" for a trip that will never run, with any paid amount never refunded.
/// </summary>
public sealed class CancelTripCommandHandler : IRequestHandler<CancelTripCommand>
{
    private const string CascadeCancellationReason = "Trip cancelled.";

    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly INotificationService _notificationService;

    public CancelTripCommandHandler(IApplicationDbContext context, IIdentityService identityService, INotificationService notificationService)
    {
        _context = context;
        _identityService = identityService;
        _notificationService = notificationService;
    }

    public async Task Handle(CancelTripCommand request, CancellationToken cancellationToken)
    {
        var trip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, cancellationToken)
            ?? throw new NotFoundException("Trip", request.TripId);

        trip.Cancel();

        var activeBookings = await _context.Bookings
            .Include(b => b.Passengers)
            .Where(b => b.TripId == request.TripId && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
            .ToListAsync(cancellationToken);

        foreach (var booking in activeBookings)
        {
            await BookingCancellationHelper.CancelAsync(
                _context, _identityService, _notificationService, booking, CascadeCancellationReason, null, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
