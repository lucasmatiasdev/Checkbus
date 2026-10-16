using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;

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

        // Scoped to (OrganizationId, Type, DocumentNumber) via a join to Vehicles —
        // VehicleDocument has no OrganizationId column of its own. excludeVehicleId lets a
        // re-upload on the same vehicle keep its own number without tripping the check.
        Task<bool> DocumentNumberExistsAsync(
            Guid organizationId,
            VehicleDocumentType type,
            string documentNumber,
            Guid excludeVehicleId,
            CancellationToken cancellationToken);
    }
}
