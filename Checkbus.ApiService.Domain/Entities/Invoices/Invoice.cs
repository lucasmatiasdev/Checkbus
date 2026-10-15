namespace Checkbus.ApiService.Domain.Entities.Invoices
{
    // One receipt per passenger, created only when the covering Payment reaches Aprobado —
    // never for an unpaid PendientePago hold (odd/tasks/pagos-mercadopago.md design decisions,
    // Fase D5). FK-only, no navigation properties, same pattern as every other entity here.
    public class Invoice
    {
        public Guid Id { get; set; }
        public required Guid TripId { get; set; }
        public required Guid PassengerId { get; set; }
        public required Guid PaymentId { get; set; }
        public required DateTime IssuedAt { get; set; }
    }
}
