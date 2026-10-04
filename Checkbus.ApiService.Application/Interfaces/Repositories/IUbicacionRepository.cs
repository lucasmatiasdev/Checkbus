using Checkbus.ApiService.Domain.Entities.Rutas;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IUbicacionRepository
    {
        // Dedupe-by-PlaceId lookup: callers (Evento/Viaje creation) must check this before
        // inserting a new Ubicacion row — the same real-world place is never duplicated.
        Task<Ubicacion?> FindByPlaceIdAsync(string placeId, CancellationToken cancellationToken);
        Task AddAsync(Ubicacion ubicacion, CancellationToken cancellationToken);
        Task<Ubicacion?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    }
}
