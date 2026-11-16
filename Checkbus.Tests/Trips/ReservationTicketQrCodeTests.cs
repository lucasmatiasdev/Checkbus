using Checkbus.ApiService.Application.Trips.Events;

namespace Checkbus.Tests.Trips;

/// <summary>
/// Proves <see cref="ReservationTicketQrCode"/>'s pure QR-image generation on the API side — the
/// copy embedded in the "reserva confirmada" email. Mirrors
/// <see cref="Checkbus.Tests.Web.ReservationTicketQrCodeTests"/>'s assertions for its Web-side
/// twin: this repo has no QR-decoding library, so these assert the observable, meaningful
/// contract (well-formed PNG bytes, determinism, distinct output for distinct input) rather than
/// the decoded payload.
/// </summary>
public class ReservationTicketQrCodeTests
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Fact]
    public void GeneratePngBytes_ReturnsPngSignature()
    {
        var bytes = ReservationTicketQrCode.GeneratePngBytes(Guid.NewGuid());

        Assert.True(bytes.Length > PngSignature.Length);
        Assert.Equal(PngSignature, bytes[..PngSignature.Length]);
    }

    [Fact]
    public void GeneratePngBytes_SameReservationId_IsDeterministic()
    {
        var reservationId = Guid.NewGuid();

        var first = ReservationTicketQrCode.GeneratePngBytes(reservationId);
        var second = ReservationTicketQrCode.GeneratePngBytes(reservationId);

        Assert.Equal(first, second);
    }

    [Fact]
    public void GeneratePngBytes_DifferentReservationIds_ProduceDifferentOutput()
    {
        var first = ReservationTicketQrCode.GeneratePngBytes(Guid.NewGuid());
        var second = ReservationTicketQrCode.GeneratePngBytes(Guid.NewGuid());

        Assert.NotEqual(first, second);
    }
}
