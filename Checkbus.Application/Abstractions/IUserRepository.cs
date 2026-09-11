using Checkbus.Domain.Entities.Authentication;

namespace Checkbus.Application.Abstractions
{
    public interface IUserRepository
    {
        Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
