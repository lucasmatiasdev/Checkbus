namespace Checkbus.ApiService.Infrastructure.Implementations.Payments
{

    public class ProcessedWebhookEvent
    {
        public Guid Id { get; set; }

        public required string ExternalEventId { get; set; }

        public required DateTime ProcessedAt { get; set; }
    }
}
