using Checkbus.ApiService.Domain.Entities.Documents;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IDriverRequirementRepository
    {
        Task<IReadOnlyList<DriverRequirement>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
        Task AddRangeAsync(IEnumerable<DriverRequirement> requirements, CancellationToken cancellationToken);
        Task UpdateAsync(DriverRequirement requirement, CancellationToken cancellationToken);

        Task<int> GetExpiringOrExpiredCountByOrganizationAsync(
            Guid organizationId, DateOnly expiringThresholdDate, CancellationToken cancellationToken);
    }
}
