using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Rutas;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Implementations.Repositories
{
    public class UbicacionRepository : IUbicacionRepository
    {
        private readonly CheckbusDbContext _context;

        public UbicacionRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        public Task<Ubicacion?> FindByPlaceIdAsync(string placeId, CancellationToken cancellationToken)
        {
            return _context.Ubicaciones.FirstOrDefaultAsync(u => u.PlaceId == placeId, cancellationToken);
        }

        public async Task AddAsync(Ubicacion ubicacion, CancellationToken cancellationToken)
        {
            _context.Ubicaciones.Add(ubicacion);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public Task<Ubicacion?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return _context.Ubicaciones.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }
    }
}
