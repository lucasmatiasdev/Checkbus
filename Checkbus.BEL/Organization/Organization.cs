namespace Checkbus.BEL.Organization;

/// <summary>
/// A tenant of the Checkbus system. The root of the shared-schema
/// multi-tenancy model: every tenant-scoped entity carries an
/// <c>OrganizationId</c> that ultimately points back here.
/// </summary>
public class Organization
{
    public int Id { get; set; }

    public required string Name { get; set; }

    /// <summary>Argentine tax id (CUIT), globally unique.</summary>
    public required string Cuit { get; set; }

    public bool IsActive { get; set; } = true;
}
