namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    // Fase D3 (odd/tasks/pagos-mercadopago.md) webhook dedupe port. Deliberately primitive-typed
    // (no entity exposed) — the backing ProcessedWebhookEvent row is pure Infrastructure
    // plumbing with no business meaning, see its own doc comment.
    public interface IProcessedWebhookEventRepository
    {
        // Checked first, before any other processing, once signature validation (D2) has
        // already passed.
        Task<bool> ExistsAsync(string externalEventId, CancellationToken cancellationToken);

        // Recorded BEFORE the rest of HandlePaymentWebhookCommandHandler's processing runs — see
        // that handler's own doc comment for the accepted record-before-vs-after tradeoff.
        Task RecordAsync(string externalEventId, CancellationToken cancellationToken);
    }
}
