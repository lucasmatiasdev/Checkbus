using QRCoder;

namespace Checkbus.Web.Features.PublicReservations.Services;

/// <summary>
/// Generates a scannable PNG rendering of a reservation's boarding code,
/// deferred CU-14 gap. The QR encodes the raw <c>Reservation.Id</c> GUID as plain text, not a
/// URL: <c>ValidationMethod.Codigo</c> (see
/// <c>Checkbus.ApiService.Domain/Enums/ValidationMethod.cs</c>) is literally the driver
/// typing/pasting that same id to validate boarding, so a QR a driver can scan-and-paste just
/// saves them typing a 36-character GUID by hand — there is no new backend endpoint, no change
/// to the boarding flow, and no camera-scanning UI here (CU-12 already decided against that).
/// Generation runs entirely server-side inside this Blazor Server app, which is why this is a
/// plain, dependency-free static class rather than an API call: no round trip to
/// Checkbus.ApiService is needed to produce an image.
/// </summary>
public static class ReservationTicketQrCode
{
    // 10 px per module keeps the resulting PNG a modest few KB while staying comfortably
    // scannable at the ~160-200px <img> size this is rendered at in ReservationConfirmation.
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
