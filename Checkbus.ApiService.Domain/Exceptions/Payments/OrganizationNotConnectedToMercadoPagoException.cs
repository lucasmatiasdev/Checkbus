using System;

namespace Checkbus.ApiService.Domain.Exceptions.Payments
{

    public class OrganizationNotConnectedToMercadoPagoException : Exception
    {
        public OrganizationNotConnectedToMercadoPagoException(string message = "Organization has no connected Mercado Pago seller account.")
            : base(message)
        {
        }
    }
}
