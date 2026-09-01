using Checkbus.DAL.Context;

namespace Checkbus.Tests.Integration;

/// <summary>Test double for <see cref="ITenantProvider"/> with mutable state.</summary>
public class FakeTenantProvider : ITenantProvider
{
    public int? CurrentOrganizationId { get; set; }

    public bool IsSystemMode { get; set; }
}
