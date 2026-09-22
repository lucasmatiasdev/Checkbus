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

        public Task<User?> FindByEmailAsync(string email)
        {
            return _context.Users
                .Include(u => u.Role)
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}
