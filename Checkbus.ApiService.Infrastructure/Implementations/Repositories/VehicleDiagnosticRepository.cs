using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Implementations.Repositories
{
    public class VehicleDiagnosticRepository : IVehicleDiagnosticRepository
    {
        private readonly CheckbusDbContext _context;

        public VehicleDiagnosticRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        // Diagnostic and components are added to the same ChangeTracker and saved in one
        // SaveChangesAsync call, so they are persisted atomically as a single aggregate.
        public async Task AddAsync(VehicleDiagnostic diagnostic, IEnumerable<ComponentDiagnostic> components, CancellationToken cancellationToken)
        {
            _context.VehicleDiagnostics.Add(diagnostic);
            _context.ComponentDiagnostics.AddRange(components);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public Task<VehicleDiagnostic?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return _context.VehicleDiagnostics.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<VehicleDiagnostic>> GetByMaintenanceRecordIdAsync(Guid maintenanceRecordId, CancellationToken cancellationToken)
        {
            return await _context.VehicleDiagnostics
                .Where(d => d.MaintenanceRecordId == maintenanceRecordId)
                .ToListAsync(cancellationToken);
        }

        // See IVehicleDiagnosticRepository.GetComponentsByDiagnosticIdsAsync — added in M3 so
        // GetVehicleDiagnosticsQueryHandler can project nested components without a separate
        // IComponentDiagnosticRepository (none exists by M1 design).
        public async Task<IReadOnlyList<ComponentDiagnostic>> GetComponentsByDiagnosticIdsAsync(IEnumerable<Guid> diagnosticIds, CancellationToken cancellationToken)
        {
            var idSet = diagnosticIds.ToList();
            return await _context.ComponentDiagnostics
                .Where(c => idSet.Contains(c.VehicleDiagnosticId))
                .ToListAsync(cancellationToken);
        }
    }
}
