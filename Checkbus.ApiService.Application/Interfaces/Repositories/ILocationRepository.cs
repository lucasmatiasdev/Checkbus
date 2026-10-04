using Checkbus.ApiService.Domain.Entities.Trips;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface ILocationRepository
    {
        // Dedupe-by-PlaceId lookup: callers (Event/Trip creation) must check this before
        // inserting a new Location row — the same real-world place is never duplicated.
        Task<Location?> FindByPlaceIdAsync(string placeId, CancellationToken cancellationToken);
        Task AddAsync(Location location, CancellationToken cancellationToken);
        Task<Location?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    }
}
