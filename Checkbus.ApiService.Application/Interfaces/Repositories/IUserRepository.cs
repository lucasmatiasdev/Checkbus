using Checkbus.ApiService.Domain.Entities.Authentication;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);
        Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
        Task AddAsync(User user, CancellationToken cancellationToken);
        Task UpdateAsync(User user, CancellationToken cancellationToken);
        Task<bool> DocumentNumberExistsInOrganizationAsync(
            string documentNumber, Guid organizationId, CancellationToken cancellationToken);
        Task<IReadOnlyList<string>> FindEmailsByLocalPartPrefixAsync(
            string localPartPrefix, string domain, CancellationToken cancellationToken);
        Task<IReadOnlyList<User>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken);
    }
}
