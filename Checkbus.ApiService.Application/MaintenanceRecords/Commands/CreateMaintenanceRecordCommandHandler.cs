using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.MaintenanceRecords.Queries;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.VehicleDocuments;
using MediatR;

namespace Checkbus.ApiService.Application.MaintenanceRecords.Commands
{
    public class CreateMaintenanceRecordCommandHandler : IRequestHandler<CreateMaintenanceRecordCommand, MaintenanceRecordDto>
    {
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IMaintenanceRecordRepository _maintenanceRecordRepository;
        private readonly ICurrentUserService _currentUser;

        public CreateMaintenanceRecordCommandHandler(
            IVehicleRepository vehicleRepository,
            IMaintenanceRecordRepository maintenanceRecordRepository,
            ICurrentUserService currentUser)
        {
            _vehicleRepository = vehicleRepository;
            _maintenanceRecordRepository = maintenanceRecordRepository;
            _currentUser = currentUser;
        }

        public async Task<MaintenanceRecordDto> Handle(CreateMaintenanceRecordCommand request, CancellationToken cancellationToken)
        {
            // Role gate (Mecanico/Administrador) is enforced at the controller; this handler
            // only needs to verify tenant ownership of the target vehicle (IDOR-safe, mirrors
            // UploadVehicleDocumentCommandHandler / CreateVehicleCommandHandler).
            var callerOrganizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId, cancellationToken);
            if (vehicle is null || vehicle.OrganizationId != callerOrganizationId)
            {
                // IDOR-safe: cross-tenant existence is never leaked to the caller.
                throw new VehicleNotFoundException();
            }

            var now = DateTime.UtcNow;
            var record = new MaintenanceRecord
            {
                Id = Guid.NewGuid(),
                VehicleId = request.VehicleId,
                Type = request.Type,
                ScheduledDate = request.ScheduledDate,
                Status = MaintenanceStatus.Programado,
                Description = request.Description,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _maintenanceRecordRepository.AddAsync(record, cancellationToken);

            return new MaintenanceRecordDto
            {
                Id = record.Id,
                VehicleId = record.VehicleId,
                Type = record.Type,
                ScheduledDate = record.ScheduledDate,
                StartDate = record.StartDate,
                EndDate = record.EndDate,
                Status = record.Status,
                Description = record.Description,
                MechanicNotes = record.MechanicNotes,
                Cost = record.Cost,
                VehiclePatent = vehicle.Patent,
                VehicleBrand = vehicle.Brand,
                VehicleModel = vehicle.Model
            };
        }
    }
}
