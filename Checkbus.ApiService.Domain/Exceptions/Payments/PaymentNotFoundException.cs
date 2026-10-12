using System;

namespace Checkbus.ApiService.Domain.Exceptions.Payments
{
    // Thrown when a lookup by Payment.Id (or by ExternalOrderId/external_reference once Fase
    // B/D wire up the Mercado Pago webhook and IPaymentRepository) does not resolve to any
    // Payment row — covers both a garbage/mistyped id and a genuinely missing Payment.
    public class PaymentNotFoundException : Exception
    {
        public PaymentNotFoundException(string message = "Payment not found.") : base(message) { }
    }
}
