namespace Checkbus.ApiService.Application.Interfaces.Repositories
{

    public interface IProcessedWebhookEventRepository
    {

        Task<bool> ExistsAsync(string externalEventId, CancellationToken cancellationToken);

        Task RecordAsync(string externalEventId, CancellationToken cancellationToken);
    }
}
