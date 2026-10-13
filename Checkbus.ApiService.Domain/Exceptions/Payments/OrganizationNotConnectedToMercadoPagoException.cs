using System;

namespace Checkbus.ApiService.Domain.Exceptions.Payments
{
    // Thrown by the Fase I token-resolution service when a Trip's Organization has no
    // MercadoPagoConnection row at all (odd/tasks/pagos-mercadopago.md Fase I, I6). There is no
    // global fallback token any more once this Fase lands — a Trip whose Organization never
    // connected its own Mercado Pago seller account cannot have a real order created or
    // refunded against it, and this must fail loudly/clearly rather than silently falling back
    // to any other account's credentials.
    public class OrganizationNotConnectedToMercadoPagoException : Exception
    {
        public OrganizationNotConnectedToMercadoPagoException(string message = "Organization has no connected Mercado Pago seller account.")
            : base(message)
        {
        }
    }
}
