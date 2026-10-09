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

        // Added for R5 (odd/tasks/reservas-publicas.md): resolves one Reservation by its own
        // unguessable 128-bit Id — the sole access-control mechanism for the public
        // confirmation page, there is no authenticated caller to scope by. Returns null for an
        // id that doesn't resolve rather than throwing; the caller (
        // GetPublicReservationsConfirmationQueryHandler) treats that as "omit this id", not an
        // error.
        Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

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
