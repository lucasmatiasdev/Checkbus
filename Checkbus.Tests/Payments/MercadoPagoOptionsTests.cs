using Checkbus.ApiService.Infrastructure.Implementations.Payments;

namespace Checkbus.Tests.Payments;

/// <summary>
/// Proves <see cref="MercadoPagoOptions"/>'s permissive-default shape (odd/tasks/pagos-mercadopago.md
/// Fase B, B3) — mirrors <c>EmailOptionsTests</c>: a brand-new instance (as built by
/// <c>Program.cs</c>'s <c>?? new MercadoPagoOptions()</c> fallback when the "MercadoPago" config
/// section is missing) must have blank string fields and never throw/require construction
/// arguments.
/// </summary>
public class MercadoPagoOptionsTests
{
    [Fact]
    public void DefaultConstruction_HasBlankStrings()
    {
        var options = new MercadoPagoOptions();

        Assert.Equal(string.Empty, options.AccessToken);
        Assert.Equal(string.Empty, options.WebhookSecret);
        // Fase I (odd/tasks/pagos-mercadopago.md): Checkbus's own platform-app OAuth credentials.
        Assert.Equal(string.Empty, options.ClientId);
        Assert.Equal(string.Empty, options.ClientSecret);
        Assert.Equal(string.Empty, options.RedirectUri);
    }
}
