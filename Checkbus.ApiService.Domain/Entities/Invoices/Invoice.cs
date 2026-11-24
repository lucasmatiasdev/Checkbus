namespace Checkbus.ApiService.Domain.Entities.Invoices
{

    public class Invoice
    {
        public Guid Id { get; set; }
        public required Guid TripId { get; set; }
        public required Guid PassengerId { get; set; }
        public required Guid PaymentId { get; set; }
        public required DateTime IssuedAt { get; set; }
    }
}
