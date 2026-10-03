using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Implementations.Repositories
{
    public class MaintenanceRecordRepository : IMaintenanceRecordRepository
    {
        private readonly CheckbusDbContext _context;

        public MaintenanceRecordRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        public Task<MaintenanceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return _context.MaintenanceRecords.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        public async Task AddAsync(MaintenanceRecord record, CancellationToken cancellationToken)
        {
            _context.MaintenanceRecords.Add(record);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(MaintenanceRecord record, CancellationToken cancellationToken)
        {
            _context.MaintenanceRecords.Update(record);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<MaintenanceRecord>> GetByVehicleIdsAsync(IEnumerable<Guid> vehicleIds, CancellationToken cancellationToken)
        {
            return await _context.MaintenanceRecords
                .Where(r => vehicleIds.Contains(r.VehicleId))
                .ToListAsync(cancellationToken);
        }
    }
}
