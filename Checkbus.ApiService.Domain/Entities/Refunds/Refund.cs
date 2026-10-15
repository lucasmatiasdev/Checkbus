using Checkbus.ApiService.Domain.ValueObjects;

namespace Checkbus.ApiService.Domain.Entities.Refunds
{
    // Deliberately references the specific Reservation it covers, not just the Payment it was
    // charged against: a Payment can cover several seats bought in one checkout, and cancelling
    // one of them (CancelReservationCommandHandler, Fase F) must issue a partial refund
    // traceable to that one seat's price, not the whole Payment — so a partial refund of 1-of-3
    // seats stays auditable (odd/tasks/pagos-mercadopago.md design decisions). FK-only, no
    // navigation properties, same pattern as every other entity here.
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
