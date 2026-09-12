using Checkbus.Application.Abstractions;
using Checkbus.Domain.Entities.Tenancy;
using Checkbus.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Infrastructure.Repositories
{
    public class OrganizationRepository : IOrganizationRepository
    {
        private readonly CheckbusDbContext _context;

        public OrganizationRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        public Task<Organization?> FindByIdAsync(int id, CancellationToken ct = default) =>
            _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == id, ct);
    }
}
