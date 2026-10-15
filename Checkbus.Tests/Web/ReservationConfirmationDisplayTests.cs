using Checkbus.Web.Components.Pages.PublicPages;

namespace Checkbus.Tests.Web;

/// <summary>
/// Proves <see cref="ReservationConfirmation"/>'s pure status-display logic added in
/// odd/tasks/pagos-mercadopago.md Fase C2 (<c>GetStatusLabel</c>/<c>CanCancel</c>). This repo
/// has no bUnit (see <see cref="RegisterUserPageAuthorizationTests"/>'s own doc comment for
/// that same constraint), so the page's markup itself cannot be rendered — these two methods
/// were deliberately made <c>public static</c> precisely so this logic is directly unit
/// testable without one.
/// </summary>
public class ReservationConfirmationDisplayTests
{
    [Theory]
    [InlineData("Confirmada", "Confirmada")]
    [InlineData("PendientePago", "Pendiente de pago")]
    [InlineData("Cancelada", "Cancelada")]
    public void GetStatusLabel_KnownStatus_ReturnsBuyerFacingLabel(string status, string expectedLabel)
    {
        Assert.Equal(expectedLabel, ReservationConfirmation.GetStatusLabel(status));
    }

    [Fact]
    public void GetStatusLabel_UnrecognizedStatus_FallsBackToRawValue()
    {
        Assert.Equal("SomethingUnexpected", ReservationConfirmation.GetStatusLabel("SomethingUnexpected"));
    }

    [Fact]
    public void CanCancel_Confirmada_ReturnsTrue()
    {
        Assert.True(ReservationConfirmation.CanCancel("Confirmada"));
    }

    [Theory]
    [InlineData("PendientePago")]
    [InlineData("Cancelada")]
    public void CanCancel_NonConfirmada_ReturnsFalse(string status)
    {
        // PendientePago (Fase C's new hold state) must not offer cancellation here —
        // cancelling an unpaid hold is a different, unscoped operation
        // (CancelReservationCommandHandler only ever handles a Confirmada reservation).
        Assert.False(ReservationConfirmation.CanCancel(status));
    }
}
