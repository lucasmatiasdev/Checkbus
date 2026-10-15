using System;

namespace Checkbus.ApiService.Domain.Exceptions.Payments
{
    // Thrown when a refund is attempted against a Payment whose Status does not allow it —
    // either still Pendiente (nothing has actually been charged/approved yet) or already fully
    // Reembolsado (there is nothing left to give back). ParcialmenteReembolsado is NOT one of
    // these rejected cases: a Payment covering several seats can be refunded more than once as
    // each seat's Reservation is cancelled independently (odd/tasks/pagos-mercadopago.md
    // Fase F).
    public class PaymentNotRefundableException : Exception
    {
        public PaymentNotRefundableException(string message = "Payment is not in a refundable state.") : base(message) { }
    }
}
