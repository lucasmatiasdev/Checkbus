using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Reservations;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IReservationRepository
    {
        // Excludes Cancelada rows (Fase 4 / follow-up to C1): a cancelled Reservation must not
        // count as occupying its Seat. Used by GetPublicTripSeatMapQueryHandler to compute
        // which Seat.Numbers are occupied for a published Trip, by
        // GetPublicTripDetailQueryHandler to compute AvailableSeats, and by
        // CreatePublicReservationCommandHandler's pre-check (R4) to reject obviously-taken
        // seats before attempting a write — filtering here, once, keeps all three callers
        // correct without each one re-deriving "active" itself, and matches the filtered
        // unique index on Reservation(TripId, SeatId) added in C1 (a Cancelada row already
        // doesn't block a new Reservation at the DB level; this method's result is now
        // consistent with that).
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

        // Added for Fase 4 (Cancelación), business-named same as
        // IBoardingRecordRepository.ValidateAsync. Sets Status = Cancelada and saves in one
        // SaveChangesAsync call — deliberately carries NO business-rule checks itself (the
        // Abordado / 24h-window policy checks are CancelReservationCommandHandler's job, same
        // "thin persistence, caller owns the rules" split every other repository in this
        // codebase follows). Throws ReservationNotFoundException if reservationId doesn't
        // resolve — defensive, not a business rule: the handler already resolved it once
        // before calling this.
        Task CancelAsync(Guid reservationId, CancellationToken cancellationToken);
    }
}
