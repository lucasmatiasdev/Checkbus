using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Implementations.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly CheckbusDbContext _context;

        public UserRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
        {
            return _context.Users
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken)
        {
            // Raw DbUpdateException propagates on either unique-index violation
            // (Email, or the composite OrganizationId+DocumentNumber), mirroring
            // OrganizationRepositoryTests' pattern — the Application layer has no EF
            // Core dependency, so translating this into a domain exception, if ever
            // needed, belongs at this boundary, not inside RegisterUserCommandHandler.
            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public Task<bool> DocumentNumberExistsInOrganizationAsync(
            string documentNumber, Guid organizationId, CancellationToken cancellationToken)
        {
            return _context.Users.AnyAsync(
                u => u.DocumentNumber == documentNumber && u.OrganizationId == organizationId,
                cancellationToken);
        }

        public async Task<IReadOnlyList<string>> FindEmailsByLocalPartPrefixAsync(
            string localPartPrefix, string domain, CancellationToken cancellationToken)
        {
            // Deliberately over-fetches (e.g. "jose.diazxyz@d" matches the prefix); safe
            // because UserEmailGenerator.Generate checks exact candidate membership, not
            // prefix membership (D-5). Never reuse FindByEmailAsync's aggregate-loading
            // .Include in this disambiguation path.
            var suffix = "@" + domain;
            return await _context.Users
                .Where(u => u.Email.StartsWith(localPartPrefix) && u.Email.EndsWith(suffix))
                .Select(u => u.Email)
                .ToListAsync(cancellationToken);
        }
    }
}
