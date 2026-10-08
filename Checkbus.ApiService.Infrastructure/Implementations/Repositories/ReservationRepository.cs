using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Implementations.Repositories
{
    public class ReservationRepository : IReservationRepository
    {
        private readonly CheckbusDbContext _context;

        public ReservationRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Reservation>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
        {
            return await _context.Reservations
                .Where(r => r.TripId == tripId)
                .ToListAsync(cancellationToken);
        }
    }
}
