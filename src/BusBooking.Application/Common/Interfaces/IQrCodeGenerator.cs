namespace BusBooking.Application.Common.Interfaces;

/// <summary>Turns an opaque payload (a Ticket's TicketCode — never passenger data) into a QR
/// code image. Kept separate from ticket generation so the QR library can be swapped without
/// touching anything that decides what a ticket is.</summary>
public interface IQrCodeGenerator
{
    byte[] GeneratePng(string payload);
}
