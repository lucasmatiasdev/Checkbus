using Checkbus.Domain.Entities.Authentication;

namespace Checkbus.Application.Abstractions
{
    public interface IUserRepository
    {
        Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
        Task<UserInsertOutcome> AddAsync(User user, CancellationToken ct = default);
        Task<IReadOnlyList<string>> FindEmailsByPrefixAsync(int organizationId, string localPartPrefix, CancellationToken ct = default);
    }

    public enum UserInsertOutcome
    {
        Added,
        DuplicateEmail
    }
}
