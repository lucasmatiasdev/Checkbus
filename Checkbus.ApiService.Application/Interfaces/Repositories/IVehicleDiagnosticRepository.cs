using Checkbus.ApiService.Domain.Entities.Vehicles;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IVehicleDiagnosticRepository
    {
        // Persists the diagnostic together with its ComponentDiagnostic children in one
        // SaveChangesAsync call — there is no independent lifecycle or endpoint for
        // components, so they are always added as part of the same aggregate save.
        Task AddAsync(VehicleDiagnostic diagnostic, IEnumerable<ComponentDiagnostic> components, CancellationToken cancellationToken);
        Task<VehicleDiagnostic?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<IReadOnlyList<VehicleDiagnostic>> GetByMaintenanceRecordIdAsync(Guid maintenanceRecordId, CancellationToken cancellationToken);

        // Components have no independent repository (M1 decision) and GetByMaintenanceRecordIdAsync
        // deliberately returns diagnostics only — components stay "queryable separately" (see
        // VehicleDiagnosticRepositoryTests). Added in M3 because GetVehicleDiagnosticsQuery needs to
        // project a nested ComponentDiagnosticDto list and the Application layer has no other way to
        // reach ComponentDiagnostic rows (no EF/Infrastructure reference from Application).
        Task<IReadOnlyList<ComponentDiagnostic>> GetComponentsByDiagnosticIdsAsync(IEnumerable<Guid> diagnosticIds, CancellationToken cancellationToken);
    }
}
