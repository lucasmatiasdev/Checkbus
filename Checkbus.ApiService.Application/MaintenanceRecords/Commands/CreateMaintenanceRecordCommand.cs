using Checkbus.ApiService.Application.MaintenanceRecords.Queries;
using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.MaintenanceRecords.Commands
{
    public class CreateMaintenanceRecordCommand : IRequest<MaintenanceRecordDto>
    {
        public required Guid VehicleId { get; set; }
        public required MaintenanceType Type { get; set; }
        public required DateOnly ScheduledDate { get; set; }
        public required string Description { get; set; }

        // Deliberately ABSENT: OrganizationId — tenant scoping is resolved exclusively
        // from the authenticated caller's token inside the handler (a body-supplied value
        // would be a cross-tenant IDOR), same pattern as CreateVehicleCommand.
    }
}
