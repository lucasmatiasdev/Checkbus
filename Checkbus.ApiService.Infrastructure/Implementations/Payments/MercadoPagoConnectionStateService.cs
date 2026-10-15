using System.Text.Json;
using Checkbus.ApiService.Application.Interfaces.Payments;
using Microsoft.AspNetCore.DataProtection;

namespace Checkbus.ApiService.Infrastructure.Implementations.Payments
{
    /// <summary>
    /// <see cref="IMercadoPagoConnectionStateService"/> implementation (odd/tasks/pagos-mercadopago.md
    /// Fase I, I7) — ASP.NET Core Data Protection, same mechanism and reasoning
    /// <see cref="Checkbus.ApiService.Infrastructure.Implementations.Repositories.OrganizationMercadoPagoConnectionRepository"/>
    /// already uses, but a DISTINCT purpose string ("MercadoPago.OAuthState" vs
    /// "MercadoPagoConnection.Tokens") so a protector created for one could never accidentally
    /// unprotect a value created for the other.
    /// </summary>
    public class MercadoPagoConnectionStateService : IMercadoPagoConnectionStateService
    {
        // Mercado Pago documents its own one-time authorization code as a 10-minute TTL
        // (odd/tasks/pagos-mercadopago.md Fase I's own confirmed OAuth facts) — this state value
        // only ever needs to survive the round trip from BuildAuthorizationUrl to the callback,
        // which happens well inside that same window. Matching it here (rather than inventing an
        // unrelated number) means a state this old is already useless anyway (the code it would
        // be paired with has expired too), so rejecting it early is a clear, defensible bound.
        public const int StateTtlMinutes = 10;

        private readonly IDataProtector _protector;

        public MercadoPagoConnectionStateService(IDataProtectionProvider dataProtectionProvider)
        {
            _protector = dataProtectionProvider.CreateProtector("MercadoPago.OAuthState");
        }

        public string Protect(Guid organizationId)
        {
            var payload = JsonSerializer.Serialize(new StatePayload(organizationId, DateTime.UtcNow));
            return _protector.Protect(payload);
        }

        public MercadoPagoConnectionStateResult TryUnprotect(string protectedState)
        {
            try
            {
                var json = _protector.Unprotect(protectedState);
                var payload = JsonSerializer.Deserialize<StatePayload>(json);
                if (payload is null)
                {
                    return new MercadoPagoConnectionStateResult(false, Guid.Empty);
                }

                if (payload.IssuedAtUtc < DateTime.UtcNow.AddMinutes(-StateTtlMinutes))
                {
                    return new MercadoPagoConnectionStateResult(false, Guid.Empty);
                }

                return new MercadoPagoConnectionStateResult(true, payload.OrganizationId);
            }
            catch (Exception)
            {
                // Covers a tampered/garbage ciphertext (CryptographicException from Unprotect)
                // and a malformed/unexpected JSON payload (JsonException) identically: either way
                // this state value is untrustworthy and must never throw into the caller — the
                // anonymous OAuth callback action has no error page to render an unhandled
                // exception onto, only a redirect.
                return new MercadoPagoConnectionStateResult(false, Guid.Empty);
            }
        }

        private sealed record StatePayload(Guid OrganizationId, DateTime IssuedAtUtc);
    }
}
