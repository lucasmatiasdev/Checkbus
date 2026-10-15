using System;

namespace Checkbus.ApiService.Domain.Exceptions.Payments
{
    // Thrown when the Mercado Pago Orders API call itself fails during checkout creation
    // (CreatePublicReservationCommandHandler, Fase C) — a network/gateway-level failure, not a
    // domain validation rejection. Fase A only introduces the type; Fase B's IPaymentService/
    // MercadoPagoPaymentService implementation is what actually throws it, and Fase C3 is what
    // reacts to it by cleaning up the PendientePago batch instead of leaving an orphaned hold.
    public class OrderCreationFailedException : Exception
    {
        public OrderCreationFailedException(string message = "Failed to create the Mercado Pago order.") : base(message) { }
    }
}
