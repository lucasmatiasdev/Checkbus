using Checkbus.ApiService.Domain.Entities.Rutas;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IEventoRepository
    {
        Task AddAsync(Evento evento, CancellationToken cancellationToken);
        Task<Evento?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        // Unscoped — Evento is a shared catalog, no OrganizationId to filter by.
        Task<IReadOnlyList<Evento>> GetAllAsync(CancellationToken cancellationToken);
    }
}
