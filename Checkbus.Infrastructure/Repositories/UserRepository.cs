using Checkbus.Application.Abstractions;
using Checkbus.Domain.Entities.Authentication;
using Checkbus.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Checkbus.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private const string UniqueViolationSqlState = "23505";

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

        // D9: AddAsync saves and catches the unique-constraint violation itself — Application
        // may not name DbUpdateException/PostgresException. On 23505 the tracked entity MUST be
        // detached, otherwise a retry with the next suffix would re-insert the stale Added entry.
        public async Task<UserInsertOutcome> AddAsync(User user, CancellationToken ct = default)
        {
            _context.Users.Add(user);
            try
            {
                await _context.SaveChangesAsync(ct);
                return UserInsertOutcome.Added;
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: UniqueViolationSqlState })
            {
                _context.Entry(user).State = EntityState.Detached;
                return UserInsertOutcome.DuplicateEmail;
            }
        }

        public async Task<IReadOnlyList<string>> FindEmailsByPrefixAsync(int organizationId, string localPartPrefix, CancellationToken ct = default) =>
            await _context.Users
                .Where(u => u.OrganizationId == organizationId && u.Email.StartsWith(localPartPrefix))
                .Select(u => u.Email)
                .ToListAsync(ct);
    }
}
