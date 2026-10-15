using System.Text.Json;
using Checkbus.ApiService.Infrastructure.Implementations.Payments;
using Microsoft.AspNetCore.DataProtection;

namespace Checkbus.Tests.Payments;

/// <summary>
/// Proves <see cref="MercadoPagoConnectionStateService"/> (odd/tasks/pagos-mercadopago.md Fase I,
/// I7): a value produced by <c>Protect</c> round-trips through <c>TryUnprotect</c>, a garbage or
/// foreign-purpose value is rejected (never throws), and the 10-minute TTL is enforced. Uses
/// <see cref="EphemeralDataProtectionProvider"/>, same no-ASP.NET-Core-host-needed pattern
/// <see cref="Checkbus.Tests.Persistence.OrganizationMercadoPagoConnectionRepositoryTests"/>
/// already established for the sibling Data Protection usage in this Fase.
/// </summary>
public class MercadoPagoConnectionStateServiceTests
{
    [Fact]
    public void Protect_ThenTryUnprotect_RoundTripsOrganizationId()
    {
        var service = new MercadoPagoConnectionStateService(new EphemeralDataProtectionProvider());
        var organizationId = Guid.NewGuid();

        var state = service.Protect(organizationId);
        var result = service.TryUnprotect(state);

        Assert.True(result.IsValid);
        Assert.Equal(organizationId, result.OrganizationId);
    }

    [Fact]
    public void TryUnprotect_GarbageValue_ReturnsInvalidRatherThanThrowing()
    {
        var service = new MercadoPagoConnectionStateService(new EphemeralDataProtectionProvider());

        var result = service.TryUnprotect("not-a-real-protected-value");

        Assert.False(result.IsValid);
    }

    [Fact]
    public void TryUnprotect_ValueProtectedUnderADifferentPurpose_ReturnsInvalid()
    {
        // Proves the distinct purpose string actually matters: a value protected under
        // "MercadoPagoConnection.Tokens" (the sibling repository's own purpose) must never be
        // accepted here, even though both protectors share the same underlying key ring.
        var provider = new EphemeralDataProtectionProvider();
        var foreignProtector = provider.CreateProtector("MercadoPagoConnection.Tokens");
        var foreignValue = foreignProtector.Protect("some-unrelated-payload");
        var service = new MercadoPagoConnectionStateService(provider);

        var result = service.TryUnprotect(foreignValue);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void TryUnprotect_PayloadOlderThanTtl_ReturnsInvalid()
    {
        var provider = new EphemeralDataProtectionProvider();
        var protector = provider.CreateProtector("MercadoPago.OAuthState");
        var organizationId = Guid.NewGuid();
        var staleJson = JsonSerializer.Serialize(new
        {
            OrganizationId = organizationId,
            IssuedAtUtc = DateTime.UtcNow.AddMinutes(-(MercadoPagoConnectionStateService.StateTtlMinutes + 1))
        });
        var staleState = protector.Protect(staleJson);
        var service = new MercadoPagoConnectionStateService(provider);

        var result = service.TryUnprotect(staleState);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void TryUnprotect_PayloadWellWithinTtl_ReturnsValid()
    {
        var provider = new EphemeralDataProtectionProvider();
        var protector = provider.CreateProtector("MercadoPago.OAuthState");
        var organizationId = Guid.NewGuid();
        var freshJson = JsonSerializer.Serialize(new
        {
            OrganizationId = organizationId,
            IssuedAtUtc = DateTime.UtcNow.AddMinutes(-(MercadoPagoConnectionStateService.StateTtlMinutes - 1))
        });
        var freshState = protector.Protect(freshJson);
        var service = new MercadoPagoConnectionStateService(provider);

        var result = service.TryUnprotect(freshState);

        Assert.True(result.IsValid);
        Assert.Equal(organizationId, result.OrganizationId);
    }
}
