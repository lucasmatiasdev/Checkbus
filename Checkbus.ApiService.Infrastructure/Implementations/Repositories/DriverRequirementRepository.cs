using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Documents;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Implementations.Repositories
{
    public class DriverRequirementRepository : IDriverRequirementRepository
    {
        private readonly CheckbusDbContext _context;

        public DriverRequirementRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<DriverRequirement>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await _context.DriverRequirements
                .Where(r => r.UserId == userId)
                .ToListAsync(cancellationToken);
        }

        public async Task AddRangeAsync(IEnumerable<DriverRequirement> requirements, CancellationToken cancellationToken)
        {
            _context.DriverRequirements.AddRange(requirements);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(DriverRequirement requirement, CancellationToken cancellationToken)
        {
            _context.DriverRequirements.Update(requirement);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> GetExpiringOrExpiredCountByOrganizationAsync(
            Guid organizationId, DateOnly expiringThresholdDate, CancellationToken cancellationToken)
        {
            // DriverRequirement has no navigation back to User (FK-only relationship by
            // design), so the organization scope is applied via a subquery over Users
            // rather than a join through a navigation property.
            var organizationUserIds = _context.Users
                .Where(u => u.OrganizationId == organizationId)
                .Select(u => u.Id);

            return await _context.DriverRequirements
                .Where(r => organizationUserIds.Contains(r.UserId)
                    && r.ExpirationDate != null
                    && r.ExpirationDate <= expiringThresholdDate)
                .CountAsync(cancellationToken);
        }
    }
}
