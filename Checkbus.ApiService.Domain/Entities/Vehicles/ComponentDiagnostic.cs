using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Domain.Entities.Vehicles
{
    // One row per component finding within a VehicleDiagnostic. Always created together
    // with its parent VehicleDiagnostic in one command — no independent lifecycle, no
    // separate endpoint. FK-only relationship, no navigation property back.
    public class ComponentDiagnostic
    {
        public Guid Id { get; set; }
        public required Guid VehicleDiagnosticId { get; set; }
        public required VehicleComponent Component { get; set; }
        public required ComponentCondition Condition { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
