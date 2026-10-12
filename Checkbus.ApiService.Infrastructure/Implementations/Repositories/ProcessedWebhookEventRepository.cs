using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Infrastructure.Implementations.Payments;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Implementations.Repositories
{
    public class ProcessedWebhookEventRepository : IProcessedWebhookEventRepository
    {
        private readonly CheckbusDbContext _context;

        public ProcessedWebhookEventRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        public Task<bool> ExistsAsync(string externalEventId, CancellationToken cancellationToken)
        {
            return _context.ProcessedWebhookEvents.AnyAsync(e => e.ExternalEventId == externalEventId, cancellationToken);
        }

        public async Task RecordAsync(string externalEventId, CancellationToken cancellationToken)
        {
            _context.ProcessedWebhookEvents.Add(new ProcessedWebhookEvent
            {
                Id = Guid.NewGuid(),
                ExternalEventId = externalEventId,
                ProcessedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
