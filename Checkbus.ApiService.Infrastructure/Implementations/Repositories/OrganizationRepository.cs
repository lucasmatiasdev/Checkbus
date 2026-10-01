using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Implementations.Repositories
{
    public class OrganizationRepository : IOrganizationRepository
    {
        private readonly CheckbusDbContext _context;

        public OrganizationRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        public Task<string?> GetSlugByIdAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            return _context.Organizations
                .Where(o => o.Id == organizationId)
                .Select(o => o.Slug)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
