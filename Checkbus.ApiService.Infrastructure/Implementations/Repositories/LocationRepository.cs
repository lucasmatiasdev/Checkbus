using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Implementations.Repositories
{
    public class LocationRepository : ILocationRepository
    {
        private readonly CheckbusDbContext _context;

        public LocationRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        public Task<Location?> FindByPlaceIdAsync(string placeId, CancellationToken cancellationToken)
        {
            return _context.Locations.FirstOrDefaultAsync(l => l.PlaceId == placeId, cancellationToken);
        }

        public async Task AddAsync(Location location, CancellationToken cancellationToken)
        {
            _context.Locations.Add(location);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public Task<Location?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return _context.Locations.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        }
    }
}
