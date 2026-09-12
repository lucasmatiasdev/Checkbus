using Checkbus.Domain.Entities.Tenancy;

namespace Checkbus.Application.Abstractions
{
    public interface IOrganizationRepository
    {
        Task<Organization?> FindByIdAsync(int id, CancellationToken ct = default);
    }
}
