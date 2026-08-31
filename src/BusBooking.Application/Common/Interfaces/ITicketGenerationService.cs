namespace BusBooking.Application.Common.Interfaces;

/// <summary>Turns a just-confirmed booking's passengers into Tickets. Idempotent — safe to
/// call more than once for the same booking, generating only what's missing.</summary>
public interface ITicketGenerationService
{
    Task GenerateForBookingAsync(Guid bookingId, CancellationToken cancellationToken);
}
