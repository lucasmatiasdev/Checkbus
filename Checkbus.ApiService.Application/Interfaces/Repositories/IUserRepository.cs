using Checkbus.ApiService.Domain.Entities.Authentication;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);
        Task AddAsync(User user, CancellationToken cancellationToken);
        Task<bool> DocumentNumberExistsInOrganizationAsync(
            string documentNumber, Guid organizationId, CancellationToken cancellationToken);
        Task<IReadOnlyList<string>> FindEmailsByLocalPartPrefixAsync(
            string localPartPrefix, string domain, CancellationToken cancellationToken);
    }
}
