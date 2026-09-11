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
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        public Task SaveChangesAsync(CancellationToken ct = default) =>
            _context.SaveChangesAsync(ct);
    }
}
