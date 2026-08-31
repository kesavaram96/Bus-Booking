namespace BusBooking.Domain.Enums;

/// <summary>
/// The doc's full event list. BookingConfirmed, PaymentSuccessful, BookingCancelled and
/// UpcomingTripReminder are wired to real trigger points as of Phase 18; TripCancelled and
/// TripTimeChanged are declared here (so this column never needs a widening migration later)
/// but nothing raises them yet — see the README for why each is scoped out for now.
/// </summary>
public enum NotificationEventType
{
    BookingConfirmed = 1,
    BookingCancelled = 2,
    TripCancelled = 3,
    TripTimeChanged = 4,
    PaymentSuccessful = 5,
    UpcomingTripReminder = 6
}
