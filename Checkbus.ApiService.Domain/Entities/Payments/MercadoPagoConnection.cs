namespace Checkbus.ApiService.Domain.Entities.Payments
{

    public class MercadoPagoConnection
    {
        public Guid Id { get; set; }
        public required Guid OrganizationId { get; set; }

        public required string SellerUserId { get; set; }

        public required string AccessToken { get; set; }

        public required string RefreshToken { get; set; }

        public required DateTime ExpiresAt { get; set; }

        public required DateTime ConnectedAt { get; set; }
    }
}
