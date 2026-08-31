using BusBooking.Application.Common.Interfaces;
using BusBooking.Domain.Entities;
using BusBooking.Domain.Enums;

namespace BusBooking.Application.Notifications;

/// <summary>
/// Shared by every booking-related notification trigger (ConfirmPayment, booking/trip
/// cancellation): a registered Customer's account email takes priority; otherwise the first
/// passenger's email, falling back to their phone number (SMS) since PhoneNumber — unlike
/// Email — is required on every BookingPassenger, so this always resolves to something as long
/// as the booking has at least one passenger.
/// </summary>
public static class BookingNotificationRecipientResolver
{
    public static async Task<(string Recipient, NotificationChannel Channel)?> ResolveAsync(
        IIdentityService identityService, Booking booking, CancellationToken cancellationToken)
    {
        if (booking.CustomerId.HasValue)
        {
            var user = await identityService.FindByIdAsync(booking.CustomerId.Value, cancellationToken);
            if (user is not null)
            {
                return (user.Email, NotificationChannel.Email);
            }
        }

        var passenger = booking.Passengers.FirstOrDefault();
        if (passenger is null)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(passenger.Email)
            ? (passenger.PhoneNumber, NotificationChannel.Sms)
            : (passenger.Email!, NotificationChannel.Email);
    }
}
