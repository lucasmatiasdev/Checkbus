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

        Task<int> GetExpiringOrExpiredCountByOrganizationAsync(
            Guid organizationId, DateOnly expiringThresholdDate, CancellationToken cancellationToken);

        Task<bool> DocumentNumberExistsAsync(
            Guid organizationId,
            VehicleDocumentType type,
            string documentNumber,
            Guid excludeVehicleId,
            CancellationToken cancellationToken);
    }
}
