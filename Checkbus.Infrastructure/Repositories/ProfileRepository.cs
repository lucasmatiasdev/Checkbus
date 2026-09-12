using Checkbus.Application.Abstractions;
using Checkbus.Domain.Entities.Authentication.Authorization;
using Checkbus.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Infrastructure.Repositories
{
    public class ProfileRepository : IProfileRepository
    {
        private readonly CheckbusDbContext _context;

        public ProfileRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        // MUST Include(Organization) — Profile's organization FK is a shadow property (D5);
        // RegisterUseCase's tenancy check relies on the navigation being loaded.
        public Task<Profile?> FindByIdAsync(int id, CancellationToken ct = default) =>
            _context.Profiles
                .Include(p => p.Organization)
                .FirstOrDefaultAsync(p => p.Id == id, ct);
    }
}
