using Checkbus.ApiService.Domain.Entities.Audit;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IAuditLogRepository
    {
        Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken);
        Task<IReadOnlyList<AuditLogEntry>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken);

        // Cross-tenant listing for CheckbusSoporte (CU-16) — callers must verify the
        // sentinel-organization check in Application before using this, same caveat as
        // ISupportTicketRepository.GetAllAsync.
        Task<IReadOnlyList<AuditLogEntry>> GetAllAsync(CancellationToken cancellationToken);
    }
}
