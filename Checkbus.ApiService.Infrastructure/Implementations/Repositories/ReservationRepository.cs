using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Payments;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Reservations;
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

        // Excludes Cancelada rows — see IReservationRepository's doc comment.
        public async Task<IReadOnlyList<Reservation>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
        {
            return await _context.Reservations
                .Where(r => r.TripId == tripId && r.Status != ReservationStatus.Cancelada)
                .ToListAsync(cancellationToken);
        }

        public async Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.Reservations.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        // Payment + Passenger + Reservation rows for the whole batch are added to the same
        // ChangeTracker and saved in one SaveChangesAsync call, so they are persisted
        // atomically as a single aggregate — same convention as TripRepository.AddAsync.
        // Deliberately does NOT catch DbUpdateException here: that translation into a
        // domain-level failure belongs to the handler, keeping this repository a thin
        // persistence layer.
        public async Task CreateBatchAsync(
            Payment payment,
            IReadOnlyCollection<Passenger> passengers,
            IReadOnlyCollection<Reservation> reservations,
            CancellationToken cancellationToken)
        {
            _context.Payments.Add(payment);
            _context.Passengers.AddRange(passengers);
            _context.Reservations.AddRange(reservations);

            await _context.SaveChangesAsync(cancellationToken);
        }

        // Thin persistence only, no business-rule checks — see IReservationRepository's doc
        // comment. CancelReservationCommandHandler already resolved and checked this
        // Reservation once before calling here; the not-found throw below is purely
        // defensive.
        public async Task CancelAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken)
                ?? throw new ReservationNotFoundException();

            reservation.Status = ReservationStatus.Cancelada;

            await _context.SaveChangesAsync(cancellationToken);
        }

        // Fase D7: resolves every Reservation belonging to this Payment directly via its own
        // PaymentId FK (supersedes the Fase D PaymentReservationLink join table), regardless of
        // Status.
        public async Task<IReadOnlyList<Reservation>> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken)
        {
            return await _context.Reservations
                .Where(r => r.PaymentId == paymentId)
                .ToListAsync(cancellationToken);
        }

        // Fase D's approved-webhook path — mirror image of CancelAsync above.
        public async Task ConfirmAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken)
                ?? throw new ReservationNotFoundException();

            reservation.Status = ReservationStatus.Confirmada;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
