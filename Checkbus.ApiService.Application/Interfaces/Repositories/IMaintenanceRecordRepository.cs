using Checkbus.ApiService.Domain.Entities.Vehicles;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IMaintenanceRecordRepository
    {
        Task<MaintenanceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task AddAsync(MaintenanceRecord record, CancellationToken cancellationToken);
        Task UpdateAsync(MaintenanceRecord record, CancellationToken cancellationToken);

        // MaintenanceRecord has no direct OrganizationId column, so org-scoping is resolved
        // by the caller first (a Vehicle lookup/join, same shape as VehicleDocument's
        // expiring-count query) and the resulting vehicle ids are passed in here.
        Task<IReadOnlyList<MaintenanceRecord>> GetByVehicleIdsAsync(IEnumerable<Guid> vehicleIds, CancellationToken cancellationToken);
    }
}
