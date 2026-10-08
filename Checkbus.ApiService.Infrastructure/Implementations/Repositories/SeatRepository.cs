using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Seats;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Implementations.Repositories
{
    public class SeatRepository : ISeatRepository
    {
        private readonly CheckbusDbContext _context;

        public SeatRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        public async Task EnsureSeatsForVehicleAsync(Guid vehicleId, int capacity, CancellationToken cancellationToken)
        {
            var existingNumbers = await _context.Seats
                .Where(s => s.VehicleId == vehicleId)
                .Select(s => s.Number)
                .ToListAsync(cancellationToken);

            if (existingNumbers.Count >= capacity)
            {
                // Idempotent no-op read — seats already exist for this vehicle.
                return;
            }

            var existingSet = existingNumbers.ToHashSet();
            var missingSeats = Enumerable.Range(1, capacity)
                .Where(number => !existingSet.Contains(number))
                .Select(number => new Seat { VehicleId = vehicleId, Number = number });

            _context.Seats.AddRange(missingSeats);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Seat>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken)
        {
            return await _context.Seats
                .Where(s => s.VehicleId == vehicleId)
                .ToListAsync(cancellationToken);
        }
    }
}
