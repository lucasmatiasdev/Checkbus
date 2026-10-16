using Checkbus.Web.Features.PublicReservations.Services;

namespace Checkbus.Tests.Web;

/// <summary>
/// Proves <see cref="ReservationTicketQrCode"/>'s pure QR-image generation, added for CU-14's
/// downloadable ticket gap. This repo has no QR-decoding library and no bUnit (see
/// <see cref="RegisterUserPageAuthorizationTests"/>'s own doc comment for that same
/// bUnit constraint), so these tests cannot assert the QR's decoded payload — instead they
/// assert the observable, meaningful contract: a well-formed PNG data URL, determinism for the
/// same input, and distinct output for distinct input.
/// </summary>
public class ReservationTicketQrCodeTests
{
    [Fact]
    public void GeneratePngDataUrl_ReturnsPngDataUrlPrefix()
    {
        var reservationId = Guid.NewGuid();

        var dataUrl = ReservationTicketQrCode.GeneratePngDataUrl(reservationId);

        Assert.StartsWith("data:image/png;base64,", dataUrl);
    }

    [Fact]
    public void GeneratePngDataUrl_Base64PayloadIsNonEmptyAndDecodable()
    {
        var reservationId = Guid.NewGuid();

        var dataUrl = ReservationTicketQrCode.GeneratePngDataUrl(reservationId);
        var base64Payload = dataUrl["data:image/png;base64,".Length..];

        Assert.NotEmpty(base64Payload);
        // Convert.FromBase64String throws FormatException on malformed input — this is the
        // "decodable" half of the assertion, since this repo has no QR-decoding library to
        // verify the payload actually encodes the reservation id.
        var bytes = Convert.FromBase64String(base64Payload);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void GeneratePngDataUrl_SameReservationId_IsDeterministic()
    {
        var reservationId = Guid.NewGuid();

        var first = ReservationTicketQrCode.GeneratePngDataUrl(reservationId);
        var second = ReservationTicketQrCode.GeneratePngDataUrl(reservationId);

        Assert.Equal(first, second);
    }

    [Fact]
    public void GeneratePngDataUrl_DifferentReservationIds_ProduceDifferentOutput()
    {
        var first = ReservationTicketQrCode.GeneratePngDataUrl(Guid.NewGuid());
        var second = ReservationTicketQrCode.GeneratePngDataUrl(Guid.NewGuid());

        Assert.NotEqual(first, second);
    }
}
