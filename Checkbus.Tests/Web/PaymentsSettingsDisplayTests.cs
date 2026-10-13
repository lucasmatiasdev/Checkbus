using Checkbus.Web.Components.Pages.app;

namespace Checkbus.Tests.Web;

/// <summary>
/// Proves <see cref="PaymentsSettings"/>'s pure failure-reason display mapping
/// (odd/tasks/pagos-mercadopago.md Fase I, I8's own <c>GetReasonLabel</c>). This repo has no
/// bUnit (see <see cref="RegisterUserPageAuthorizationTests"/>'s own doc comment for that same
/// constraint), so the page's markup itself cannot be rendered — this method was deliberately
/// made <c>public static</c> precisely so this logic is directly unit testable without one, same
/// precedent <c>ReservationConfirmation.GetStatusLabel</c> already established.
/// </summary>
public class PaymentsSettingsDisplayTests
{
    [Theory]
    [InlineData("invalid_state")]
    [InlineData("exchange_failed")]
    [InlineData("missing_parameters")]
    public void GetReasonLabel_KnownReason_ReturnsNonEmptySpanishLabel(string reason)
    {
        var label = PaymentsSettings.GetReasonLabel(reason);

        Assert.False(string.IsNullOrWhiteSpace(label));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("something-unexpected")]
    public void GetReasonLabel_UnrecognizedOrMissingReason_FallsBackToGenericLabel(string? reason)
    {
        Assert.Equal("error desconocido", PaymentsSettings.GetReasonLabel(reason));
    }
}
