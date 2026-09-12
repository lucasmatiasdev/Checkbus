using Checkbus.Application.Abstractions;
using Checkbus.Domain.Entities.Authentication;
using Checkbus.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly CheckbusDbContext _context;

        public UserRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        public Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken ct = default) =>
            _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, ct);

        public Task SaveChangesAsync(CancellationToken ct = default) =>
            _context.SaveChangesAsync(ct);

        // TODO(user-registration Phase 3, D9): real implementation catches DbUpdateException/
        // PostgresException 23505, detaches the tracked entity, and returns DuplicateEmail.
        // Stub only exists so IUserRepository compiles after the Phase 1 port change; no
        // production caller exists until RegisterUseCase lands in Phase 2.
        public Task<UserInsertOutcome> AddAsync(User user, CancellationToken ct = default) =>
            throw new NotImplementedException("Implemented in user-registration Phase 3 (D9).");

        // TODO(user-registration Phase 3): real implementation filters on the CLR OrganizationId
        // column. Stub only exists so IUserRepository compiles after the Phase 1 port change.
        public Task<IReadOnlyList<string>> FindEmailsByPrefixAsync(int organizationId, string localPartPrefix, CancellationToken ct = default) =>
            throw new NotImplementedException("Implemented in user-registration Phase 3.");
    }
}
