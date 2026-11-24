using QRCoder;

namespace Checkbus.Web.Features.PublicReservations.Services;

/// <summary>
/// Generates a scannable PNG rendering of a reservation's boarding code,
/// deferred CU-14 gap. The QR encodes the raw <c>Reservation.Id</c> GUID as plain text, not a
/// URL: <c>ValidationMethod.Codigo</c>/<c>Qr</c> (see
/// <c>Checkbus.ApiService.Domain/Enums/ValidationMethod.cs</c>) both resolve the same id, typed
/// or scanned — the boarding screen's camera scanner (odd/tasks/validacion-embarque-qr.md) reads
/// this exact PNG. Generation runs entirely server-side inside this Blazor Server app, which is
/// why this is a plain, dependency-free static class rather than an API call: no round trip to
/// Checkbus.ApiService is needed to produce an image.
/// </summary>
public static class ReservationTicketQrCode
{

    private const int PixelsPerModule = 10;

    public static string GeneratePngDataUrl(Guid reservationId)
    {
        using var generator = new QRCodeGenerator();
        using var qrCodeData = generator.CreateQrCode(reservationId.ToString(), QRCodeGenerator.ECCLevel.Q);
        using var pngQrCode = new PngByteQRCode(qrCodeData);
        var pngBytes = pngQrCode.GetGraphic(PixelsPerModule);

        return $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";
    }
}
