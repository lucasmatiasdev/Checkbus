using Checkbus.ApiService.Domain.Entities.Vehicles;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IVehicleDocumentRepository
    {
        Task<IReadOnlyList<VehicleDocument>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken);
        Task AddRangeAsync(IEnumerable<VehicleDocument> documents, CancellationToken cancellationToken);
        Task AddAsync(VehicleDocument document, CancellationToken cancellationToken);
        Task UpdateAsync(VehicleDocument document, CancellationToken cancellationToken);

        // Covers BOTH "already expired" and "expires within the threshold window" in one
        // condition: ExpirationDate != null && ExpirationDate <= expiringThresholdDate.
        // The caller passes today.AddDays(30) as the threshold, so anything on or before
        // that date is either already expired or about to expire within 30 days.
        Task<int> GetExpiringOrExpiredCountByOrganizationAsync(
            Guid organizationId, DateOnly expiringThresholdDate, CancellationToken cancellationToken);
    }
}
