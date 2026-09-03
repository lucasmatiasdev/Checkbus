using Checkbus.BEL.Fleet.Enums;

namespace Checkbus.BEL.Fleet;

/// <summary>
/// A diagnostic check performed on a <see cref="Vehicle"/> at a point in
/// time. <see cref="OverallState"/> is the worst (highest-value)
/// <see cref="ComponentDiagnostic.Rating"/> among <see cref="Components"/>;
/// computing it is BLL responsibility, not enforced at this layer.
/// </summary>
public class VehicleDiagnostic
{
    public int Id { get; set; }

    public int VehicleId { get; set; }

    public Vehicle Vehicle { get; set; } = null!;

    public int Mileage { get; set; }

    public ComponentRating OverallState { get; set; }

    public DateTime RecordedAt { get; set; }

    public ICollection<ComponentDiagnostic> Components { get; set; } = new List<ComponentDiagnostic>();
}
