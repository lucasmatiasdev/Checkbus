using Checkbus.ApiService.Application.VehicleDiagnostics.Queries;
using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.VehicleDiagnostics.Commands
{

    public sealed record ComponentDiagnosticInput(VehicleComponent Component, ComponentCondition Condition, string? Notes);

    public class CreateVehicleDiagnosticCommand : IRequest<VehicleDiagnosticDto>
    {
        public required Guid MaintenanceRecordId { get; set; }
        public required DateOnly DiagnosedAt { get; set; }
        public string? Notes { get; set; }
        public required List<ComponentDiagnosticInput> Components { get; set; }

    }
}
