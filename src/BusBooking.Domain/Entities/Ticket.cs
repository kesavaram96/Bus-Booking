using System.Security.Cryptography;

namespace BusBooking.Domain.Entities;

/// <summary>
/// One per BookingPassenger (each passenger boards/alights at their own stops and needs their
/// own scannable ticket), generated once their booking is confirmed. Unlike Payment, a Ticket
/// is tightly coupled to its Booking on purpose — it has no independent lifecycle of its own.
/// </summary>
public class Ticket : Common.BaseAuditableEntity
{
    public Guid BookingId { get; private set; }

    public Booking Booking { get; private set; } = default!;

    public Guid BookingPassengerId { get; private set; }

    public BookingPassenger BookingPassenger { get; private set; } = default!;

    public string TicketNumber { get; private set; } = default!;

    /// <summary>The opaque, unguessable value encoded into the QR — deliberately not the row's
    /// own Id, so the externally shared verification credential is never the same value as an
    /// internal database key. Never derived from or containing passenger data.</summary>
    public string TicketCode { get; private set; } = default!;

    private Ticket()
    {
    }

    public Ticket(Guid bookingId, Guid bookingPassengerId)
    {
        if (bookingId == Guid.Empty)
        {
            throw new ArgumentException("Booking id is required.", nameof(bookingId));
        }

        if (bookingPassengerId == Guid.Empty)
        {
            throw new ArgumentException("Booking passenger id is required.", nameof(bookingPassengerId));
        }

        BookingId = bookingId;
        BookingPassengerId = bookingPassengerId;
        TicketNumber = GenerateTicketNumber();
        TicketCode = GenerateTicketCode();
    }

    private static string GenerateTicketNumber() =>
        $"TKT{DateTime.UtcNow:yyMMdd}{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    private static string GenerateTicketCode() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
}
