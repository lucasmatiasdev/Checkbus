using Checkbus.ApiService.Domain.Entities.Tenancy;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IOrganizationRepository
    {

        Task AddAsync(Organization organization, CancellationToken cancellationToken);

        Task<string?> GetSlugByIdAsync(Guid organizationId, CancellationToken cancellationToken);

        Task<string?> GetNameByIdAsync(Guid organizationId, CancellationToken cancellationToken);

        Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken);

        Task<bool> CuitExistsAsync(string cuit, CancellationToken cancellationToken);
        Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken);

        Task<IReadOnlyList<Organization>> GetAllAsync(CancellationToken cancellationToken);
    }
}
