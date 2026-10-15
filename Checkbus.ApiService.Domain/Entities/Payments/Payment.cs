using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.ValueObjects;

namespace Checkbus.ApiService.Domain.Entities.Payments
{
    // One Payment covers one Mercado Pago checkout (one Orders API "order"), which can span
    // several seats/Reservations bought together in the same batch
    // CreatePublicReservationCommandHandler already supports — Payment is 1:N with Reservation
    // (odd/tasks/pagos-mercadopago.md design decisions). ExternalOrderId stays null until Fase
    // C's handler actually gets a successful response back from Mercado Pago's order-creation
    // call — the row (and its PendientePago Reservations) exist before that succeeds, so a
    // failed order-creation call has something concrete to clean up (OrderCreationFailedException,
    // Fase C3). FK-only, no navigation properties, same pattern as every other entity here.
    public class Payment
    {
        public Guid Id { get; set; }
        public string? ExternalOrderId { get; set; }
        public required Money TotalAmount { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pendiente;
        public required DateTime CreatedAt { get; set; }
    }
}
