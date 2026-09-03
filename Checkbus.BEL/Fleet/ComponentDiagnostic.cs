using Checkbus.BEL.Fleet.Enums;

namespace Checkbus.BEL.Fleet;

/// <summary>
/// The diagnostic rating of a single vehicle component, part of a
/// <see cref="VehicleDiagnostic"/>. Tenant scope flows through
/// <c>VehicleDiagnostic.Vehicle</c>.
/// </summary>
public class ComponentDiagnostic
{
    public int Id { get; set; }

    public int VehicleDiagnosticId { get; set; }

    public VehicleDiagnostic VehicleDiagnostic { get; set; } = null!;

    public VehicleComponent Component { get; set; }

    public ComponentRating Rating { get; set; }
}
