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

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task UpdateAsync(User user, CancellationToken cancellationToken)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken)
        {

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

            var suffix = "@" + domain;
            return await _context.Users
                .Where(u => u.Email.StartsWith(localPartPrefix) && u.Email.EndsWith(suffix))
                .Select(u => u.Email)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            return await _context.Users
                .Where(u => u.OrganizationId == organizationId)
                .ToListAsync(cancellationToken);
        }
    }
}
