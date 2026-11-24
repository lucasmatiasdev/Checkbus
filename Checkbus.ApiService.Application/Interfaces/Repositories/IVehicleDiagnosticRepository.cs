using Checkbus.ApiService.Domain.Entities.Vehicles;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IVehicleDiagnosticRepository
    {

        Task AddAsync(VehicleDiagnostic diagnostic, IEnumerable<ComponentDiagnostic> components, CancellationToken cancellationToken);
        Task<VehicleDiagnostic?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<IReadOnlyList<VehicleDiagnostic>> GetByMaintenanceRecordIdAsync(Guid maintenanceRecordId, CancellationToken cancellationToken);

        Task<IReadOnlyList<ComponentDiagnostic>> GetComponentsByDiagnosticIdsAsync(IEnumerable<Guid> diagnosticIds, CancellationToken cancellationToken);
    }
}
