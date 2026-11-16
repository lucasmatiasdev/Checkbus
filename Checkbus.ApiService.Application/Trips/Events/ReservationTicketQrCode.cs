using QRCoder;

namespace Checkbus.ApiService.Application.Trips.Events;

/// <summary>
/// Generates the scannable PNG for a reservation's boarding code, embedded in the "reserva
/// confirmada" email <see cref="PaymentApprovedEventHandler"/> sends. Same encoding as
/// <c>Checkbus.Web.Features.PublicReservations.Services.ReservationTicketQrCode</c> (the one
/// rendered on the public confirmation page): the QR encodes the raw <c>Reservation.Id</c> GUID
/// as plain text, not a URL — <c>ValidationMethod.Codigo</c>/<c>Qr</c> both resolve the same id,
/// typed or scanned from the boarding screen's camera (odd/tasks/validacion-embarque-qr.md).
/// Duplicated rather than shared because Checkbus.Web
/// (pure BFF) and Checkbus.ApiService.Application never reference each other — same precedent as
/// <c>WebValidationMethod</c> mirroring <c>ValidationMethod</c>. Returns raw PNG bytes (not a
/// data URL) because the email embeds it as a MIME inline resource (<c>cid:</c> reference), not
/// an <c>&lt;img src="data:..."&gt;</c> — several mail clients strip base64 data URIs from HTML
/// bodies, but all of them resolve <c>cid:</c> references to an attached part.
/// </summary>
public static class ReservationTicketQrCode
{
    private const int PixelsPerModule = 10;

    public static byte[] GeneratePngBytes(Guid reservationId)
    {
        using var generator = new QRCodeGenerator();
        using var qrCodeData = generator.CreateQrCode(reservationId.ToString(), QRCodeGenerator.ECCLevel.Q);
        using var pngQrCode = new PngByteQRCode(qrCodeData);

        return pngQrCode.GetGraphic(PixelsPerModule);
    }
}
