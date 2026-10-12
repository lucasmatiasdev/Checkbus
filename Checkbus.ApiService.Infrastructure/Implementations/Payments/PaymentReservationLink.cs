namespace Checkbus.ApiService.Infrastructure.Implementations.Payments
{
    // Fase D (odd/tasks/pagos-mercadopago.md) needs to resolve "every Reservation belonging to
    // one Payment" (1 Payment : N Reservation, per this feature's design decisions) when a
    // webhook notification arrives — but Reservation itself (Domain, Fase A) carries no
    // PaymentId FK; Fases A/B/C never needed that direction, only Payment -> order id.
    //
    // Adding a PaymentId column to Reservation and populating it would require editing
    // Reservation.cs (not listed in this Fase's allowed edit surfaces) and
    // CreatePublicReservationCommandHandler.cs (explicitly listed as "do NOT touch ... stop and
    // report instead of editing"). This link table is the alternative that touches neither: a
    // brand-new table, populated by one small, additive line inside
    // ReservationRepository.CreateBatchAsync's own body (not its signature, not its callers, not
    // Reservation itself — see that method's comment). Infrastructure-only, same reasoning as
    // ProcessedWebhookEvent: this has no business meaning Application needs to reason about
    // directly, it is purely how IReservationRepository.GetByPaymentIdAsync is implemented.
    public class PaymentReservationLink
    {
        public Guid Id { get; set; }
        public required Guid PaymentId { get; set; }
        public required Guid ReservationId { get; set; }
    }
}
