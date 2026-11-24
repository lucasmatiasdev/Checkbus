using System;

namespace Checkbus.ApiService.Domain.Exceptions.Payments
{

    public class OrderCreationFailedException : Exception
    {
        public OrderCreationFailedException(string message = "Failed to create the Mercado Pago order.") : base(message) { }
    }
}
