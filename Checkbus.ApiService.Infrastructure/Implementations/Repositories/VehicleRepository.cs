using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Implementations.Repositories
{
    public class VehicleRepository : IVehicleRepository
    {
        private readonly CheckbusDbContext _context;

        public VehicleRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        }

        public async Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
        {
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Vehicle>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            return await _context.Vehicles
                .Where(v => v.OrganizationId == organizationId)
                .ToListAsync(cancellationToken);
        }

        public Task<bool> PatentExistsAsync(string patent, CancellationToken cancellationToken)
        {
            return _context.Vehicles.AnyAsync(v => v.Patent == patent, cancellationToken);
        }
    }
}
