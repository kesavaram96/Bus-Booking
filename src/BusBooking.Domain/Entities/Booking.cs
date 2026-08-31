using BusBooking.Domain.Enums;

namespace BusBooking.Domain.Entities;

public class Booking : Common.BaseAuditableEntity
{
    private readonly List<BookingPassenger> _passengers = [];

    public string BookingNumber { get; private set; } = default!;

    public Guid TripId { get; private set; }

    /// <summary>Null for a guest booking or a staff-created manual booking with no linked account.</summary>
    public Guid? CustomerId { get; private set; }

    public BookingStatus Status { get; private set; }

    /// <summary>Sum of passenger fares — kept in sync by AddPassenger, never set directly.</summary>
    public decimal TotalAmount { get; private set; }

    public IReadOnlyCollection<BookingPassenger> Passengers => _passengers.AsReadOnly();

    public string? CancellationReason { get; private set; }

    /// <summary>Whoever performed the cancellation — null for a system-triggered cancellation
    /// (e.g. its trip being cancelled), never null for a customer/staff-initiated one.</summary>
    public Guid? CancelledBy { get; private set; }

    public DateTime? CancelledAt { get; private set; }

    private Booking()
    {
    }

    public Booking(Guid tripId, Guid? customerId)
    {
        if (tripId == Guid.Empty)
        {
            throw new ArgumentException("Trip id is required.", nameof(tripId));
        }

        BookingNumber = GenerateBookingNumber();
        TripId = tripId;
        CustomerId = customerId;
        Status = BookingStatus.Pending;
        TotalAmount = 0m;
    }

    public void AddPassenger(BookingPassenger passenger)
    {
        ArgumentNullException.ThrowIfNull(passenger);

        _passengers.Add(passenger);
        TotalAmount += passenger.Fare;
    }

    /// <summary>Called once a Payment for this booking settles (Phase 14) — a booking is never
    /// confirmed by any other path.</summary>
    public void Confirm()
    {
        if (Status != BookingStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending booking can be confirmed.");
        }

        Status = BookingStatus.Confirmed;
    }

    /// <summary>Only from Pending/Confirmed — a booking that's already Cancelled/Refunded, or
    /// one whose trip already Completed/NoShow'd it, cannot be cancelled again.</summary>
    public void Cancel(string cancellationReason, Guid? cancelledBy)
    {
        if (Status != BookingStatus.Pending && Status != BookingStatus.Confirmed)
        {
            throw new InvalidOperationException($"A {Status} booking cannot be cancelled.");
        }

        if (string.IsNullOrWhiteSpace(cancellationReason))
        {
            throw new ArgumentException("Cancellation reason is required.", nameof(cancellationReason));
        }

        Status = BookingStatus.Cancelled;
        CancellationReason = cancellationReason.Trim();
        CancelledBy = cancelledBy;
        CancelledAt = DateTime.UtcNow;
    }

    /// <summary>A further transition on top of Cancel() — called once whatever was paid on
    /// this booking has actually been refunded, so Refunded always implies Cancelled first.</summary>
    public void MarkRefunded()
    {
        if (Status != BookingStatus.Cancelled)
        {
            throw new InvalidOperationException("Only a cancelled booking can be marked refunded.");
        }

        Status = BookingStatus.Refunded;
    }

    private static string GenerateBookingNumber() =>
        $"BK{DateTime.UtcNow:yyMMdd}{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
}
