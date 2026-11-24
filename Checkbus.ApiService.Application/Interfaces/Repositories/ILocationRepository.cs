using Checkbus.ApiService.Domain.Entities.Trips;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface ILocationRepository
    {

        Task<Location?> FindByPlaceIdAsync(string placeId, CancellationToken cancellationToken);
        Task AddAsync(Location location, CancellationToken cancellationToken);
        Task<Location?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    }
}
