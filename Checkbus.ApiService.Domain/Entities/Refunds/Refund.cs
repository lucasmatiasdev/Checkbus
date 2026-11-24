using Checkbus.ApiService.Domain.ValueObjects;

namespace Checkbus.ApiService.Domain.Entities.Refunds
{

    public class Refund
    {
        public Guid Id { get; set; }
        public required Guid PaymentId { get; set; }
        public required Guid ReservationId { get; set; }
        public required Money Amount { get; set; }
        public string? ExternalRefundId { get; set; }
        public required DateTime CreatedAt { get; set; }
    }
}
