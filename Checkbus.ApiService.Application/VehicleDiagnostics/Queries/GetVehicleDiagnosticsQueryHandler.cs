using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Exceptions.Maintenance;
using MediatR;

namespace Checkbus.ApiService.Application.VehicleDiagnostics.Queries
{
    public class GetVehicleDiagnosticsQueryHandler : IRequestHandler<GetVehicleDiagnosticsQuery, IReadOnlyList<VehicleDiagnosticDto>>
    {
        private readonly IMaintenanceRecordRepository _maintenanceRecordRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IVehicleDiagnosticRepository _vehicleDiagnosticRepository;
        private readonly ICurrentUserService _currentUser;

        public GetVehicleDiagnosticsQueryHandler(
            IMaintenanceRecordRepository maintenanceRecordRepository,
            IVehicleRepository vehicleRepository,
            IVehicleDiagnosticRepository vehicleDiagnosticRepository,
            ICurrentUserService currentUser)
        {
            _maintenanceRecordRepository = maintenanceRecordRepository;
            _vehicleRepository = vehicleRepository;
            _vehicleDiagnosticRepository = vehicleDiagnosticRepository;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<VehicleDiagnosticDto>> Handle(GetVehicleDiagnosticsQuery request, CancellationToken cancellationToken)
        {
            // Role gate (Mecanico/Administrador) is enforced at the controller; this handler
            // verifies tenant ownership by resolving the parent MaintenanceRecord's vehicle
            // (odd/tasks/vehicle-maintenance.md Constraint 1) — same org-scope shape as
            // CreateVehicleDiagnosticCommandHandler and the M2 handlers.
            var callerOrganizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var record = await _maintenanceRecordRepository.GetByIdAsync(request.MaintenanceRecordId, cancellationToken);
            if (record is null)
            {
                throw new MaintenanceRecordNotFoundException();
            }

            var vehicle = await _vehicleRepository.GetByIdAsync(record.VehicleId, cancellationToken);
            if (vehicle is null || vehicle.OrganizationId != callerOrganizationId)
            {
                // IDOR-safe: cross-tenant existence of the maintenance record is never leaked —
                // reported as the same not-found outcome as a genuinely missing record.
                throw new MaintenanceRecordNotFoundException();
            }

            var diagnostics = await _vehicleDiagnosticRepository.GetByMaintenanceRecordIdAsync(request.MaintenanceRecordId, cancellationToken);
            if (diagnostics.Count == 0)
            {
                return [];
            }

            var components = await _vehicleDiagnosticRepository.GetComponentsByDiagnosticIdsAsync(
                diagnostics.Select(d => d.Id), cancellationToken);

            var componentsByDiagnosticId = components
                .GroupBy(c => c.VehicleDiagnosticId)
                .ToDictionary(g => g.Key, g => g.ToList());

            return diagnostics
                .Select(d => MapToDto(d, componentsByDiagnosticId))
                .ToList();
        }

        private static VehicleDiagnosticDto MapToDto(
            VehicleDiagnostic diagnostic,
            Dictionary<Guid, List<ComponentDiagnostic>> componentsByDiagnosticId)
        {
            componentsByDiagnosticId.TryGetValue(diagnostic.Id, out var components);

            return new VehicleDiagnosticDto
            {
                Id = diagnostic.Id,
                VehicleId = diagnostic.VehicleId,
                MaintenanceRecordId = diagnostic.MaintenanceRecordId,
                DiagnosedAt = diagnostic.DiagnosedAt,
                Notes = diagnostic.Notes,
                Components = (components ?? [])
                    .Select(c => new ComponentDiagnosticDto
                    {
                        Id = c.Id,
                        Component = c.Component,
                        Condition = c.Condition,
                        Notes = c.Notes
                    })
                    .ToList()
            };
        }
    }
}
