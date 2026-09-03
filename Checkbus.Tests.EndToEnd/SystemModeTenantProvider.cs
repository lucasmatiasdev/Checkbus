using Checkbus.DAL.Context;

namespace Checkbus.Tests.EndToEnd;

/// <summary>
/// Minimal <see cref="ITenantProvider"/> double used only to seed test data
/// directly (bypassing tenant query filters) and to verify saved rows after
/// a real browser-driven scenario runs. Deliberately not shared with
/// <c>Checkbus.Tests</c> — this project stays a self-contained, dependency-
/// light end-to-end suite (see decision recorded in apply-progress for S3.8).
/// </summary>
internal sealed class SystemModeTenantProvider : ITenantProvider
{
    public int? CurrentOrganizationId => null;

    public bool IsSystemMode => true;
}
