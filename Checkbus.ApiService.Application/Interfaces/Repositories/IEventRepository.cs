using Checkbus.ApiService.Domain.Entities.Trips;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IEventRepository
    {
        Task AddAsync(Event @event, CancellationToken cancellationToken);
        Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task UpdateAsync(Event @event, CancellationToken cancellationToken);
        Task<IReadOnlyList<Event>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken);
    }
}
