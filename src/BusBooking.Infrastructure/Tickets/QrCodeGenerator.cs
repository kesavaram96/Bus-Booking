using BusBooking.Application.Common.Interfaces;
using QRCoder;

namespace BusBooking.Infrastructure.Tickets;

/// <summary>
/// Uses QRCoder's PngByteQRCode renderer specifically — unlike its Bitmap-based QRCode class,
/// it doesn't depend on System.Drawing/GDI+, so it works the same on Linux as on Windows.
/// </summary>
public sealed class QrCodeGenerator : IQrCodeGenerator
{
    public byte[] GeneratePng(string payload)
    {
        using var qrCodeGenerator = new QRCodeGenerator();
        using var qrCodeData = qrCodeGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var pngQrCode = new PngByteQRCode(qrCodeData);

        return pngQrCode.GetGraphic(20);
    }
}
