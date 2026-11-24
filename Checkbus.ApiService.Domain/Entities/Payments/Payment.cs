using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.ValueObjects;

namespace Checkbus.ApiService.Domain.Entities.Payments
{

    public class Payment
    {
        public Guid Id { get; set; }
        public string? ExternalOrderId { get; set; }
        public required Money TotalAmount { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pendiente;
        public required DateTime CreatedAt { get; set; }
    }
}
