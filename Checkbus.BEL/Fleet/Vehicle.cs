using Checkbus.BEL.Fleet.Enums;

namespace Checkbus.BEL.Fleet;

/// <summary>
/// A vehicle owned/operated by an organization. Tenant-scoped. Its overall
/// diagnostic state is denormalized here as the worst
/// <see cref="ComponentDiagnostic.Rating"/> among its most recent
/// <see cref="VehicleDiagnostic"/>; recomputing/enforcing it (e.g. blocking
/// trip assignment while "Critico") is business logic deferred to the BLL
/// layer, not implemented in this change.
/// </summary>
public class Vehicle
{
    public int Id { get; set; }

    public int OrganizationId { get; set; }

    /// <summary>License plate, unique per organization.</summary>
    public required string Plate { get; set; }

    public int Capacity { get; set; }

    public ComponentRating? CurrentDiagnosticState { get; set; }

    public ICollection<VehicleDocumentation> Documentations { get; set; } = new List<VehicleDocumentation>();

    public ICollection<VehicleDiagnostic> Diagnostics { get; set; } = new List<VehicleDiagnostic>();

    public ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();
}
