using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Rutas;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Implementations.Repositories
{
    public class ViajeRepository : IViajeRepository
    {
        private readonly CheckbusDbContext _context;

        public ViajeRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        // Viaje, Ruta and all Stops are added to the same ChangeTracker and saved in one
        // SaveChangesAsync call, so they are persisted atomically as a single aggregate.
        public async Task AddAsync(Viaje viaje, Ruta ruta, IEnumerable<Stop> stops, CancellationToken cancellationToken)
        {
            _context.Viajes.Add(viaje);
            _context.Rutas.Add(ruta);
            _context.Stops.AddRange(stops);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public Task<Viaje?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return _context.Viajes.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Viaje>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            return await _context.Viajes
                .Where(v => v.OrganizationId == organizationId)
                .ToListAsync(cancellationToken);
        }

        // Standard interval-overlap condition: existing.FechaSalida <= fechaLlegada &&
        // existing.FechaLlegada >= fechaSalida. A trip ending exactly when another starts
        // (or vice versa) counts as overlapping (boundary is inclusive on both sides).
        public async Task<IReadOnlyList<Viaje>> GetOverlappingByVehicleIdAsync(
            Guid vehicleId, DateTime fechaSalida, DateTime fechaLlegada, CancellationToken cancellationToken)
        {
            return await _context.Viajes
                .Where(v => v.VehicleId == vehicleId
                    && v.FechaSalida <= fechaLlegada
                    && v.FechaLlegada >= fechaSalida)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Viaje>> GetOverlappingByChoferIdAsync(
            Guid choferId, DateTime fechaSalida, DateTime fechaLlegada, CancellationToken cancellationToken)
        {
            return await _context.Viajes
                .Where(v => v.ChoferId == choferId
                    && v.FechaSalida <= fechaLlegada
                    && v.FechaLlegada >= fechaSalida)
                .ToListAsync(cancellationToken);
        }

        public Task<Ruta?> GetRutaByViajeIdAsync(Guid viajeId, CancellationToken cancellationToken)
        {
            return _context.Rutas.FirstOrDefaultAsync(r => r.ViajeId == viajeId, cancellationToken);
        }

        public async Task<IReadOnlyList<Stop>> GetStopsByRutaIdAsync(Guid rutaId, CancellationToken cancellationToken)
        {
            return await _context.Stops
                .Where(s => s.RutaId == rutaId)
                .OrderBy(s => s.Orden)
                .ToListAsync(cancellationToken);
        }
    }
}
