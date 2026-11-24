using System;

namespace Checkbus.ApiService.Domain.Exceptions.Payments
{

    public class PaymentNotRefundableException : Exception
    {
        public PaymentNotRefundableException(string message = "Payment is not in a refundable state.") : base(message) { }
    }
}
