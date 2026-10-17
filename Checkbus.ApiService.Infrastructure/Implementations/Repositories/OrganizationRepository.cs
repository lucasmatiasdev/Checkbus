using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Tenancy;
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

        public async Task AddAsync(Organization organization, CancellationToken cancellationToken)
        {
            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public Task<string?> GetSlugByIdAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            return _context.Organizations
                .Where(o => o.Id == organizationId)
                .Select(o => o.Slug)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public Task<string?> GetNameByIdAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            return _context.Organizations
                .Where(o => o.Id == organizationId)
                .Select(o => o.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            var organization = await _context.Organizations
                .Where(o => o.Slug == slug)
                .Select(o => new { o.Id })
                .FirstOrDefaultAsync(cancellationToken);

            return organization?.Id;
        }

        public Task<bool> CuitExistsAsync(string cuit, CancellationToken cancellationToken)
        {
            return _context.Organizations.AnyAsync(o => o.CUIT == cuit, cancellationToken);
        }

        public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken)
        {
            return _context.Organizations.AnyAsync(o => o.Slug == slug, cancellationToken);
        }
    }
}
