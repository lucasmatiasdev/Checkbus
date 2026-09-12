using Checkbus.Application.Abstractions;

namespace Checkbus.UnitTests.TestDoubles
{
    /// <summary>Test double for <see cref="ICurrentTenant"/> that always resolves to the same tenant.</summary>
    public sealed class FixedTenant : ICurrentTenant
    {
        public FixedTenant(int? organizationId)
        {
            OrganizationId = organizationId;
        }

        public int? OrganizationId { get; }
    }
}
