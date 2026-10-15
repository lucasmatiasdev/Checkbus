namespace Checkbus.ApiService.Infrastructure.Implementations.Payments
{
    // Fase D3 (odd/tasks/pagos-mercadopago.md) webhook inbox/dedupe row. Deliberately an
    // Infrastructure-only EF-mapped class, NOT a Domain entity: unlike Payment/Invoice/Refund
    // (which model real money/seat state the Application layer reasons about), this row has no
    // business meaning at all — it exists purely so a Mercado Pago retry of a notification this
    // process already started handling is recognized as a no-op. Application never sees this
    // class; it only talks to IProcessedWebhookEventRepository's two primitive-typed methods
    // (ExistsAsync/RecordAsync).
    public class ProcessedWebhookEvent
    {
        public Guid Id { get; set; }

        // The webhook JSON body's own top-level "id" field (the Mercado Pago notification id) —
        // NOT data.id (the order id). Unique: this is the whole point of the table.
        public required string ExternalEventId { get; set; }

        public required DateTime ProcessedAt { get; set; }
    }
}
