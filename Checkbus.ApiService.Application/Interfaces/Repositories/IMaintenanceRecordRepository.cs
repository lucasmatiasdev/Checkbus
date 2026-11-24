using Checkbus.ApiService.Domain.Entities.Vehicles;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IMaintenanceRecordRepository
    {
        Task<MaintenanceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task AddAsync(MaintenanceRecord record, CancellationToken cancellationToken);
        Task UpdateAsync(MaintenanceRecord record, CancellationToken cancellationToken);

        Task<IReadOnlyList<MaintenanceRecord>> GetByVehicleIdsAsync(IEnumerable<Guid> vehicleIds, CancellationToken cancellationToken);
    }
}
