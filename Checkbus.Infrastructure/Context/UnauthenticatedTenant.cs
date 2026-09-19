using Checkbus.Application.Abstractions;

namespace Checkbus.Infrastructure.Context
{
    /// <summary>
    /// Fail-closed default tenant (design D5). Used before <see cref="CheckbusDbContext.BindTenant"/>
    /// is called, and explicitly for migrations, seeds, and anonymous/background work. Its null
    /// OrganizationId makes ITenantEntity-filtered queries return zero rows and
    /// IOptionalTenantEntity-filtered queries return only global (null-organization) rows — never
    /// throws.
    /// </summary>
    public sealed class UnauthenticatedTenant : ICurrentTenant
    {
        public static readonly UnauthenticatedTenant Instance = new();

        private UnauthenticatedTenant()
        {
        }

        public int? OrganizationId => null;

        public string? OrganizationName => null;
    }
}
