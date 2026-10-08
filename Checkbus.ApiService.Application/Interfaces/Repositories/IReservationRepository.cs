using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Reservations;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IReservationRepository
    {
        // Any Status (right now only Confirmada exists) — used by
        // GetPublicTripSeatMapQueryHandler to compute which Seat.Numbers are occupied for a
        // published Trip, and by CreatePublicReservationCommandHandler's pre-check (R4) to
        // reject obviously-taken seats before attempting a write.
        Task<IReadOnlyList<Reservation>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken);

        // Persists one whole reservation batch (one Passenger + one Reservation per booked
        // Seat, e.g. a family of 3) in a SINGLE SaveChangesAsync call — same atomicity
        // convention as ITripRepository.AddAsync (Trip+Route+Stops in one call), so EF's
        // implicit transaction rolls back the entire batch if anything fails, including a
        // unique-constraint violation on Reservation(TripId, SeatId) (the real double-booking
        // guard — see CheckbusDbContext.OnModelCreating). Thin persistence only: the caller
        // (CreatePublicReservationCommandHandler) is responsible for catching the resulting
        // DbUpdateException and translating it into a domain-level failure — this repository
        // never catches it itself, matching every other repository in this codebase.
        Task CreateBatchAsync(
            IReadOnlyCollection<Passenger> passengers,
            IReadOnlyCollection<Reservation> reservations,
            CancellationToken cancellationToken);
    }
}
