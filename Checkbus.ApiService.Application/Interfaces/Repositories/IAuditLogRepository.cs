using Checkbus.ApiService.Domain.Entities.Audit;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IAuditLogRepository
    {
        Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken);
        Task<IReadOnlyList<AuditLogEntry>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken);

        Task<IReadOnlyList<AuditLogEntry>> GetAllAsync(CancellationToken cancellationToken);
    }
}
