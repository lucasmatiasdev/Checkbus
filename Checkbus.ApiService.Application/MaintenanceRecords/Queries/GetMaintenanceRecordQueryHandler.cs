using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Exceptions.Maintenance;
using MediatR;

namespace Checkbus.ApiService.Application.MaintenanceRecords.Queries
{
    public class GetMaintenanceRecordQueryHandler : IRequestHandler<GetMaintenanceRecordQuery, MaintenanceRecordDto>
    {
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IMaintenanceRecordRepository _maintenanceRecordRepository;
        private readonly ICurrentUserService _currentUser;

        public GetMaintenanceRecordQueryHandler(
            IVehicleRepository vehicleRepository,
            IMaintenanceRecordRepository maintenanceRecordRepository,
            ICurrentUserService currentUser)
        {
            _vehicleRepository = vehicleRepository;
            _maintenanceRecordRepository = maintenanceRecordRepository;
            _currentUser = currentUser;
        }

        public async Task<MaintenanceRecordDto> Handle(GetMaintenanceRecordQuery request, CancellationToken cancellationToken)
        {
            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var record = await _maintenanceRecordRepository.GetByIdAsync(request.Id, cancellationToken);
            if (record is null)
            {
                throw new MaintenanceRecordNotFoundException();
            }

            var vehicle = await _vehicleRepository.GetByIdAsync(record.VehicleId, cancellationToken);
            if (vehicle is null || vehicle.OrganizationId != organizationId)
            {
                // IDOR-safe: a record belonging to another organization looks identical to
                // a missing one — cross-tenant existence is never leaked to the caller.
                throw new MaintenanceRecordNotFoundException();
            }

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
