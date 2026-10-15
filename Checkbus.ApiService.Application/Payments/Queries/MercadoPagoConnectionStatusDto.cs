namespace Checkbus.ApiService.Application.Payments.Queries
{
    public class MercadoPagoConnectionStatusDto
    {
        public bool Connected { get; set; }

        // Mercado Pago's own collector/seller id (MercadoPagoConnection.SellerUserId), shown for
        // support/reconciliation purposes only — never AccessToken/RefreshToken, which never
        // leave the repository/resolver boundary at all.
        public string? SellerUserId { get; set; }
    }
}
