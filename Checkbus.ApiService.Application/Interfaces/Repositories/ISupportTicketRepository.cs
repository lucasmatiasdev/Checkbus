using Checkbus.ApiService.Domain.Entities.Support;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface ISupportTicketRepository
    {
        Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken);
        Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<IReadOnlyList<SupportTicket>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken);

        // Cross-tenant listing + status mutation for the Checkbus internal staff panel
        // (odd/tasks/panel-soporte-checkbus.md T2) - callers must verify the sentinel-organization
        // check in Application before using these, they intentionally bypass tenant scoping.
        Task<IReadOnlyList<SupportTicket>> GetAllAsync(CancellationToken cancellationToken);
        Task UpdateAsync(SupportTicket ticket, CancellationToken cancellationToken);
    }
}
