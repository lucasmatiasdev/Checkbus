using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Implementations.Repositories
{
    public class VehicleDocumentRepository : IVehicleDocumentRepository
    {
        private readonly CheckbusDbContext _context;

        public VehicleDocumentRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<VehicleDocument>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken)
        {
            return await _context.VehicleDocuments
                .Where(d => d.VehicleId == vehicleId)
                .ToListAsync(cancellationToken);
        }

        public async Task AddRangeAsync(IEnumerable<VehicleDocument> documents, CancellationToken cancellationToken)
        {
            _context.VehicleDocuments.AddRange(documents);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task AddAsync(VehicleDocument document, CancellationToken cancellationToken)
        {
            _context.VehicleDocuments.Add(document);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(VehicleDocument document, CancellationToken cancellationToken)
        {
            _context.VehicleDocuments.Update(document);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> GetExpiringOrExpiredCountByOrganizationAsync(
            Guid organizationId, DateOnly expiringThresholdDate, CancellationToken cancellationToken)
        {
            // VehicleDocument has no navigation back to Vehicle (FK-only relationship by
            // design), so the organization scope is applied via a subquery over Vehicles
            // rather than a join through a navigation property.
            var organizationVehicleIds = _context.Vehicles
                .Where(v => v.OrganizationId == organizationId)
                .Select(v => v.Id);

            return await _context.VehicleDocuments
                .Where(d => organizationVehicleIds.Contains(d.VehicleId)
                    && d.ExpirationDate != null
                    && d.ExpirationDate <= expiringThresholdDate)
                .CountAsync(cancellationToken);
        }

        public Task<bool> DocumentNumberExistsAsync(
            Guid organizationId,
            VehicleDocumentType type,
            string documentNumber,
            Guid excludeVehicleId,
            CancellationToken cancellationToken)
        {
            // Same subquery-over-Vehicles pattern as GetExpiringOrExpiredCountByOrganizationAsync:
            // VehicleDocument has no navigation back to Vehicle, so the organization scope is
            // applied via a subquery rather than a join through a navigation property.
            var organizationVehicleIds = _context.Vehicles
                .Where(v => v.OrganizationId == organizationId)
                .Select(v => v.Id);

            return _context.VehicleDocuments.AnyAsync(
                d => d.VehicleId != excludeVehicleId
                    && organizationVehicleIds.Contains(d.VehicleId)
                    && d.Type == type
                    && d.DocumentNumber == documentNumber,
                cancellationToken);
        }
    }
}
