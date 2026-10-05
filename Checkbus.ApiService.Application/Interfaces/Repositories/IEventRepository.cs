using Checkbus.ApiService.Domain.Entities.Trips;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IEventRepository
    {
        Task AddAsync(Event @event, CancellationToken cancellationToken);
        Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        // Unscoped — Event is a shared catalog, no OrganizationId to filter by.
        Task<IReadOnlyList<Event>> GetAllAsync(CancellationToken cancellationToken);
    }
}
