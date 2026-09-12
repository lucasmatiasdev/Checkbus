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

        // D7: already performs the explicit cross-tenant check at RegisterUseCase.cs:61;
        // filtering here would silently change tested behavior.
        public Task<Profile?> FindByIdAsync(int id, CancellationToken ct = default) =>
            _context.Profiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == id, ct);
    }
}
