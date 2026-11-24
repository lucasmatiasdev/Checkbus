namespace Checkbus.ApiService.Application.Payments.Queries
{
    public class MercadoPagoConnectionStatusDto
    {
        public bool Connected { get; set; }

        public string? SellerUserId { get; set; }
    }
}
