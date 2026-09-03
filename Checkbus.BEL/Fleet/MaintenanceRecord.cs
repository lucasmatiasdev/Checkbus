using Checkbus.BEL.Fleet.Enums;

namespace Checkbus.BEL.Fleet;

/// <summary>
/// A maintenance event recorded against a <see cref="Vehicle"/> (decision
/// #20.3 — retained as a confirmed operational entity). Tenant scope flows
/// through the required <see cref="Vehicle"/> navigation.
/// </summary>
public class MaintenanceRecord
{
    public int Id { get; set; }

    public int VehicleId { get; set; }

    public Vehicle Vehicle { get; set; } = null!;

    public DateTime EventDate { get; set; }

    public required string Description { get; set; }

    /// <summary>
    /// Free-text description of the affected component(s) — the spec allows
    /// more than one component per maintenance event, so this is not a single
    /// <see cref="VehicleComponent"/> enum value.
    /// </summary>
    public required string AffectedComponents { get; set; }

    public MaintenanceStatus Status { get; set; }
}
