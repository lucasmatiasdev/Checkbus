using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using MediatR;

namespace Checkbus.ApiService.Application.MaintenanceRecords.Queries
{
    public class GetMaintenanceRecordsQueryHandler : IRequestHandler<GetMaintenanceRecordsQuery, IReadOnlyList<MaintenanceRecordDto>>
    {
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IMaintenanceRecordRepository _maintenanceRecordRepository;
        private readonly ICurrentUserService _currentUser;

        public GetMaintenanceRecordsQueryHandler(
            IVehicleRepository vehicleRepository,
            IMaintenanceRecordRepository maintenanceRecordRepository,
            ICurrentUserService currentUser)
        {
            _vehicleRepository = vehicleRepository;
            _maintenanceRecordRepository = maintenanceRecordRepository;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<MaintenanceRecordDto>> Handle(GetMaintenanceRecordsQuery request, CancellationToken cancellationToken)
        {
            // Tenant comes exclusively from the validated claim — mirrors
            // GetVehiclesQueryHandler. MaintenanceRecord has no direct OrganizationId
            // column, so org-scoping always goes through this Vehicle lookup first
            // (odd/tasks/vehicle-maintenance.md Constraint 1).
            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var vehicles = await _vehicleRepository.GetAllByOrganizationAsync(organizationId, cancellationToken);
            var vehiclesById = vehicles.ToDictionary(v => v.Id);

            // A VehicleId filter naming a vehicle outside the caller's org never leaks
            // cross-tenant existence: it simply yields no matching ids below, the same
            // IDOR-safe effect as VehicleNotFoundException without a special-cased throw.
            List<Guid> vehicleIds;
            if (request.VehicleId.HasValue)
            {
                vehicleIds = vehiclesById.ContainsKey(request.VehicleId.Value)
                    ? [request.VehicleId.Value]
                    : [];
            }
            else
            {
                vehicleIds = vehiclesById.Keys.ToList();
            }

            var records = await _maintenanceRecordRepository.GetByVehicleIdsAsync(vehicleIds, cancellationToken);

            return records
                .Where(r => request.Status is null || r.Status == request.Status)
                .Where(r => request.Type is null || r.Type == request.Type)
                .Select(r => MapToDto(r, vehiclesById))
                .ToList();
        }

        private static MaintenanceRecordDto MapToDto(MaintenanceRecord record, Dictionary<Guid, Vehicle> vehiclesById)
        {
            vehiclesById.TryGetValue(record.VehicleId, out var vehicle);

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
                VehiclePatent = vehicle?.Patent,
                VehicleBrand = vehicle?.Brand,
                VehicleModel = vehicle?.Model
            };
        }
    }
}
