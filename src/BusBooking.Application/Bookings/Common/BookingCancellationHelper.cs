using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Notifications;
using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Application.Bookings.Common;

/// <summary>
/// The one place "cancel this booking" happens, shared by the direct CancelBooking command and
/// TripsController's CancelTrip cascade — the same "don't duplicate booking logic" rule already
/// applied to Booking creation. Does not call SaveChangesAsync; the caller commits alongside
/// whatever else it changed (the Booking itself, or the Trip too) in one transaction. Requires
/// booking.Passengers to already be loaded (for notification recipient resolution).
/// </summary>
public static class BookingCancellationHelper
{
    public static async Task CancelAsync(
        IApplicationDbContext context,
        IIdentityService identityService,
        INotificationService notificationService,
        Booking booking,
        string cancellationReason,
        Guid? cancelledBy,
        CancellationToken cancellationToken)
    {
        booking.Cancel(cancellationReason, cancelledBy);

        // "Update payment/refund status": a booking that was actually paid for gets its
        // Payment refunded and its own status taken one step further, Cancelled -> Refunded.
        // A booking with no Paid payment (never paid, or payment still Pending/Failed) simply
        // stays Cancelled — there's nothing to refund.
        var paidPayment = await context.Payments
            .FirstOrDefaultAsync(p => p.BookingId == booking.Id && p.Status == PaymentStatus.Paid, cancellationToken);

        if (paidPayment is not null)
        {
            paidPayment.Refund();
            booking.MarkRefunded();
        }

        // Deliberately no seat/Redis-lock release here: by the time a Booking exists at all,
        // CreateBookingCommandHandler has already released its passengers' holds (Phase 13) —
        // there is nothing left tied to this booking to release. A cancelled booking's segments
        // stop counting toward the seat-overlap check automatically, since that check already
        // excludes Cancelled bookings (Phase 13) — no extra code is needed to "free the seat".

        var recipient = await BookingNotificationRecipientResolver.ResolveAsync(identityService, booking, cancellationToken);
        if (recipient is not null)
        {
            await notificationService.NotifyAsync(
                new NotificationRequest(
                    recipient.Value.Recipient,
                    recipient.Value.Channel,
                    NotificationEventType.BookingCancelled,
                    "Your booking was cancelled",
                    $"Booking {booking.BookingNumber} was cancelled. Reason: {cancellationReason}"),
                cancellationToken);
        }
    }
}
