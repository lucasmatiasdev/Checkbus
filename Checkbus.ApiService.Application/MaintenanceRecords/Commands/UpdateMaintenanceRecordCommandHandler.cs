using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Exceptions.Maintenance;
using MediatR;

namespace Checkbus.ApiService.Application.MaintenanceRecords.Commands
{
    public class UpdateMaintenanceRecordCommandHandler : IRequestHandler<UpdateMaintenanceRecordCommand>
    {
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IMaintenanceRecordRepository _maintenanceRecordRepository;
        private readonly ICurrentUserService _currentUser;

        public UpdateMaintenanceRecordCommandHandler(
            IVehicleRepository vehicleRepository,
            IMaintenanceRecordRepository maintenanceRecordRepository,
            ICurrentUserService currentUser)
        {
            _vehicleRepository = vehicleRepository;
            _maintenanceRecordRepository = maintenanceRecordRepository;
            _currentUser = currentUser;
        }

        public async Task Handle(UpdateMaintenanceRecordCommand request, CancellationToken cancellationToken)
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

            record.Status = request.Status;
            record.StartDate = request.StartDate;
            record.EndDate = request.EndDate;
            record.MechanicNotes = request.MechanicNotes;
            record.Cost = request.Cost;
            record.UpdatedAt = DateTime.UtcNow;

            await _maintenanceRecordRepository.UpdateAsync(record, cancellationToken);
        }
    }
}
