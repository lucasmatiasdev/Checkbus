using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Passengers;
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

        public async Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.Reservations.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        // Passenger + Reservation rows for the whole batch are added to the same ChangeTracker
        // and saved in one SaveChangesAsync call, so they are persisted atomically as a single
        // aggregate — same convention as TripRepository.AddAsync. Deliberately does NOT catch
        // DbUpdateException here: that translation into a domain-level failure belongs to the
        // handler, keeping this repository a thin persistence layer.
        public async Task CreateBatchAsync(
            IReadOnlyCollection<Passenger> passengers,
            IReadOnlyCollection<Reservation> reservations,
            CancellationToken cancellationToken)
        {
            _context.Passengers.AddRange(passengers);
            _context.Reservations.AddRange(reservations);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
