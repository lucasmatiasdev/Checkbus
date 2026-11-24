using System;

namespace Checkbus.ApiService.Domain.Exceptions.Payments
{

    public class PaymentNotFoundException : Exception
    {
        public PaymentNotFoundException(string message = "Payment not found.") : base(message) { }
    }
}
