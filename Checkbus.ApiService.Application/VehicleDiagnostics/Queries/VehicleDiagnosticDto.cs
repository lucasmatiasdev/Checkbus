using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Application.VehicleDiagnostics.Queries
{
    public class ComponentDiagnosticDto
    {
        public required Guid Id { get; set; }
        public required VehicleComponent Component { get; set; }
        public required ComponentCondition Condition { get; set; }
        public string? Notes { get; set; }
    }

    public class VehicleDiagnosticDto
    {
        public required Guid Id { get; set; }
        public required Guid VehicleId { get; set; }
        public required Guid MaintenanceRecordId { get; set; }
        public DateOnly DiagnosedAt { get; set; }
        public string? Notes { get; set; }
        public List<ComponentDiagnosticDto> Components { get; set; } = [];
    }
}
