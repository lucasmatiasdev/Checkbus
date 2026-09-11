using Checkbus.Domain.Entities.Tenancy;

namespace Checkbus.Domain.Interfaces
{
    public interface IOptionalTenantEntity
    {
        public Organization? Organization { get; set; }
    }
}
