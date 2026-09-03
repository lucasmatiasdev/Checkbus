using Checkbus.BEL.Fleet.Enums;

namespace Checkbus.BEL.Fleet;

/// <summary>
/// A compliance/administrative document tracked for a <see cref="Vehicle"/>
/// (e.g. Seguro, VTV). Tenant scope flows through the required
/// <see cref="Vehicle"/> navigation.
/// </summary>
public class VehicleDocumentation
{
    public int Id { get; set; }

    public int VehicleId { get; set; }

    public Vehicle Vehicle { get; set; } = null!;

    public VehicleDocumentationType Type { get; set; }

    /// <summary>Document number, unique per vehicle.</summary>
    public required string DocumentNumber { get; set; }

    public VehicleDocumentationState State { get; set; }

    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
