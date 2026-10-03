using Checkbus.ApiService.Domain.Entities.Vehicles;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IVehicleRepository
    {
        Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken);
        Task<IReadOnlyList<Vehicle>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken);
        Task<bool> PatentExistsAsync(string patent, CancellationToken cancellationToken);
    }
}
