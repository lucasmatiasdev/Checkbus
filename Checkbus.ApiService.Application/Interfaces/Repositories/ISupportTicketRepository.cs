using Checkbus.ApiService.Domain.Entities.Support;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface ISupportTicketRepository
    {
        Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken);
        Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<IReadOnlyList<SupportTicket>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken);

        Task<IReadOnlyList<SupportTicket>> GetAllAsync(CancellationToken cancellationToken);
        Task UpdateAsync(SupportTicket ticket, CancellationToken cancellationToken);
    }
}
