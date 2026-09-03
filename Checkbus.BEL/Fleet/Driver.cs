namespace Checkbus.BEL.Fleet;

/// <summary>
/// A driver employed by an organization. Tenant-scoped (belongs to exactly
/// one <see cref="Organization.Organization"/>). Has at most one
/// <see cref="License"/> (1—1) and may have supporting documents attached via
/// <see cref="Attachment"/> (e.g. license scan, ID document).
/// </summary>
public class Driver
{
    public int Id { get; set; }

    public int OrganizationId { get; set; }

    public required string FullName { get; set; }

    /// <summary>Argentine national identity document number, unique per organization.</summary>
    public required string Dni { get; set; }

    public License? License { get; set; }

    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
