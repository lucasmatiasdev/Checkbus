using Checkbus.ApiService.Domain.Entities.Documents;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IDriverRequirementRepository
    {
        Task<IReadOnlyList<DriverRequirement>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
        Task AddRangeAsync(IEnumerable<DriverRequirement> requirements, CancellationToken cancellationToken);
        Task UpdateAsync(DriverRequirement requirement, CancellationToken cancellationToken);

        // Covers BOTH "already expired" and "expires within the threshold window" in one
        // condition: ExpirationDate != null && ExpirationDate <= expiringThresholdDate.
        // The caller passes today.AddDays(30) as the threshold, so anything on or before
        // that date is either already expired or about to expire within 30 days.
        Task<int> GetExpiringOrExpiredCountByOrganizationAsync(
            Guid organizationId, DateOnly expiringThresholdDate, CancellationToken cancellationToken);
    }
}
