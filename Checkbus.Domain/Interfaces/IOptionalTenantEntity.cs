using Checkbus.Domain.Entities.Tenancy;

namespace Checkbus.Domain.Interfaces
{
    public interface IOptionalTenantEntity
    {
        public int? OrganizationId { get; set; }
        public Organization? Organization { get; set; }
    }
}
