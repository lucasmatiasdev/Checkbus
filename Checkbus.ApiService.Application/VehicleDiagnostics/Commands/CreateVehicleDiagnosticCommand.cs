using Checkbus.ApiService.Application.VehicleDiagnostics.Queries;
using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.VehicleDiagnostics.Commands
{
    // Nested shape for one component finding. These never have an independent lifecycle or
    // endpoint — always submitted together with their parent VehicleDiagnostic in one command
    // (odd/tasks/vehicle-maintenance.md Constraint on ComponentDiagnostic), so there is no
    // separate command/request type for them.
    public sealed record ComponentDiagnosticInput(VehicleComponent Component, ComponentCondition Condition, string? Notes);

    public class CreateVehicleDiagnosticCommand : IRequest<VehicleDiagnosticDto>
    {
        public required Guid MaintenanceRecordId { get; set; }
        public required DateOnly DiagnosedAt { get; set; }
        public string? Notes { get; set; }
        public required List<ComponentDiagnosticInput> Components { get; set; }

        // Deliberately ABSENT: VehicleId — resolved exclusively from the parent
        // MaintenanceRecord inside the handler (a diagnostic can never target a different
        // vehicle than the maintenance record it belongs to), and OrganizationId — tenant
        // scoping is resolved exclusively from the authenticated caller's token, same pattern
        // as CreateMaintenanceRecordCommand.
    }
}
