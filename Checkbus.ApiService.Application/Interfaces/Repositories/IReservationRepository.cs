using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Payments;
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

        // Persists one whole reservation batch (one Payment covering the checkout, one
        // Passenger + one Reservation per booked Seat, e.g. a family of 3) in a SINGLE
        // SaveChangesAsync call — same atomicity convention as ITripRepository.AddAsync
        // (Trip+Route+Stops in one call), so EF's implicit transaction rolls back the entire
        // batch if anything fails, including a unique-constraint violation on
        // Reservation(TripId, SeatId) (the real double-booking guard — see
        // CheckbusDbContext.OnModelCreating). Payment joined the batch in
        // odd/tasks/pagos-mercadopago.md Fase C: CreatePublicReservationCommandHandler calls
        // IPaymentService.CreateOrderAsync BEFORE this method runs, so by the time this is
        // called the Mercado Pago order already exists — nothing here can fail "after" the
        // gateway call in a way that leaves an orphaned local row needing its own rollback.
        // Thin persistence only: the caller is responsible for catching the resulting
        // DbUpdateException and translating it into a domain-level failure — this repository
        // never catches it itself, matching every other repository in this codebase.
        Task CreateBatchAsync(
            Payment payment,
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

        // Added for Fase D (odd/tasks/pagos-mercadopago.md): resolves every Reservation
        // belonging to one Payment (1 Payment : N Reservation, batch purchase), regardless of
        // Status — unlike GetByTripIdAsync, this does NOT exclude Cancelada, because the
        // webhook handler needs to see a Cancelada row to detect the orphaned-approval case
        // (the hold was already released before the approval notification arrived). Reservation
        // itself carries no PaymentId FK (Fase A/B/C never needed that direction) — see
        // PaymentReservationLink's own doc comment for why this is backed by a link table
        // instead of a new column on Reservation/a change to CreatePublicReservationCommandHandler.
        //
        // Default body throws so the ~9 existing Fake IReservationRepository test doubles
        // elsewhere in this codebase (none of which exercise Fase D) do not need to be touched
        // just to add a method they never call — only ReservationRepository and this Fase's own
        // new fakes override it. Relies on C# default interface implementations; callers must
        // go through the interface type (every production caller already does, via DI).
        Task<IReadOnlyList<Reservation>> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken)
            => throw new NotSupportedException("GetByPaymentIdAsync is not implemented by this IReservationRepository.");

        // Added for Fase D's approved-webhook path: the mirror image of CancelAsync above,
        // flips Status to Confirmada. Same thin-persistence, no-business-rule-checks posture;
        // same default-body reasoning as GetByPaymentIdAsync above.
        Task ConfirmAsync(Guid reservationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("ConfirmAsync is not implemented by this IReservationRepository.");
    }
}
