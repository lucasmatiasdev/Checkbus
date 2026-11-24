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

                return new MercadoPagoConnectionStateResult(false, Guid.Empty);
            }
        }

        private sealed record StatePayload(Guid OrganizationId, DateTime IssuedAtUtc);
    }
}
