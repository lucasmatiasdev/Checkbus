using Checkbus.BEL.Fleet.Enums;

namespace Checkbus.BEL.Fleet;

/// <summary>
/// A <see cref="Driver"/>'s driving license. 1—1 with <see cref="Driver"/>;
/// tenant scope flows through the required <see cref="Driver"/> navigation
/// rather than duplicating an <c>OrganizationId</c> column.
/// </summary>
public class License
{
    public int Id { get; set; }

    public int DriverId { get; set; }

    public Driver Driver { get; set; } = null!;

    public LicenseCategory Category { get; set; }

    public DateTime ExpiryDate { get; set; }
}
