using BusBooking.Application.Bookings.Common;
using BusBooking.Application.Bookings.DTOs;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Domain.Enums;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ValidationException = BusBooking.Application.Common.Exceptions.ValidationException;

namespace BusBooking.Application.Bookings.Commands.CancelBooking;

/// <summary>
/// Supports both actor paths the doc asks for: business staff (any booking, any time up until
/// its trip completes) and a customer (only their own booking, and only outside the
/// configurable cancellation window). A guest booking (no linked CustomerId) can only be
/// cancelled by staff in this phase — self-service guest cancellation would need a secure
/// out-of-band credential (e.g. a link emailed at booking time) that doesn't exist until
/// Phase 18's notification infrastructure.
/// </summary>
public sealed class CancelBookingCommandHandler : IRequestHandler<CancelBookingCommand, BookingDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly INotificationService _notificationService;
    private readonly CancellationPolicySettings _cancellationPolicy;

    public CancelBookingCommandHandler(
        IApplicationDbContext context,
        IIdentityService identityService,
        INotificationService notificationService,
        IOptions<CancellationPolicySettings> cancellationPolicy)
    {
        _context = context;
        _identityService = identityService;
        _notificationService = notificationService;
        _cancellationPolicy = cancellationPolicy.Value;
    }

    public async Task<BookingDto> Handle(CancelBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings
            .Include(b => b.Passengers).ThenInclude(p => p.Seat)
            .Include(b => b.Passengers).ThenInclude(p => p.PickupStop)
            .Include(b => b.Passengers).ThenInclude(p => p.DropOffStop)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken)
            ?? throw new NotFoundException("Booking", request.BookingId);

        if (!request.IsStaffCancellation && booking.CustomerId != request.CancelledBy)
        {
            throw new ForbiddenAccessException("You can only cancel your own bookings.");
        }

        var trip = await _context.Trips
            .FirstOrDefaultAsync(t => t.Id == booking.TripId, cancellationToken)
            ?? throw new NotFoundException("Trip", booking.TripId);

        if (trip.Status == TripStatus.Completed)
        {
            throw new ValidationException([new ValidationFailure(nameof(request.BookingId), "A booking on a completed trip cannot be cancelled.")]);
        }

        if (!request.IsStaffCancellation)
        {
            var hoursUntilDeparture = (trip.DepartureDateTime - DateTime.UtcNow).TotalHours;
            if (hoursUntilDeparture < _cancellationPolicy.MinimumHoursBeforeDeparture)
            {
                throw new ValidationException(
                [
                    new ValidationFailure(
                        nameof(request.BookingId),
                        $"Cancellations must be made at least {_cancellationPolicy.MinimumHoursBeforeDeparture} hour(s) before departure. Please contact support.")
                ]);
            }
        }

        await BookingCancellationHelper.CancelAsync(
            _context, _identityService, _notificationService, booking, request.CancellationReason, request.CancelledBy, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return booking.ToDto();
    }
}
