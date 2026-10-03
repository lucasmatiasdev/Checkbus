using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.VehicleDiagnostics.Queries;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Exceptions.Maintenance;
using MediatR;

namespace Checkbus.ApiService.Application.VehicleDiagnostics.Commands
{
    public class CreateVehicleDiagnosticCommandHandler : IRequestHandler<CreateVehicleDiagnosticCommand, VehicleDiagnosticDto>
    {
        private readonly IMaintenanceRecordRepository _maintenanceRecordRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IVehicleDiagnosticRepository _vehicleDiagnosticRepository;
        private readonly ICurrentUserService _currentUser;

        public CreateVehicleDiagnosticCommandHandler(
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

        public async Task<VehicleDiagnosticDto> Handle(CreateVehicleDiagnosticCommand request, CancellationToken cancellationToken)
        {
            // Role gate (Mecanico/Administrador) is enforced at the controller; this handler
            // verifies tenant ownership by resolving the parent MaintenanceRecord's vehicle
            // (odd/tasks/vehicle-maintenance.md Constraint 1) — same org-scope shape as M2's
            // CreateMaintenanceRecordCommandHandler, one level deeper.
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

            var now = DateTime.UtcNow;
            var diagnostic = new VehicleDiagnostic
            {
                Id = Guid.NewGuid(),
                VehicleId = record.VehicleId,
                MaintenanceRecordId = record.Id,
                DiagnosedAt = request.DiagnosedAt,
                Notes = request.Notes,
                CreatedAt = now,
                UpdatedAt = now
            };

            var components = request.Components
                .Select(input => new ComponentDiagnostic
                {
                    Id = Guid.NewGuid(),
                    VehicleDiagnosticId = diagnostic.Id,
                    Component = input.Component,
                    Condition = input.Condition,
                    Notes = input.Notes,
                    CreatedAt = now,
                    UpdatedAt = now
                })
                .ToList();

            // Diagnostic and its components are persisted atomically in one aggregate save —
            // see IVehicleDiagnosticRepository.AddAsync.
            await _vehicleDiagnosticRepository.AddAsync(diagnostic, components, cancellationToken);

            return new VehicleDiagnosticDto
            {
                Id = diagnostic.Id,
                VehicleId = diagnostic.VehicleId,
                MaintenanceRecordId = diagnostic.MaintenanceRecordId,
                DiagnosedAt = diagnostic.DiagnosedAt,
                Notes = diagnostic.Notes,
                Components = components
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
