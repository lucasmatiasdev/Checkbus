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

        public async Task CancelAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken)
                ?? throw new ReservationNotFoundException();

            reservation.Status = ReservationStatus.Cancelada;

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Reservation>> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken)
        {
            return await _context.Reservations
                .Where(r => r.PaymentId == paymentId)
                .ToListAsync(cancellationToken);
        }

        public async Task ConfirmAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken)
                ?? throw new ReservationNotFoundException();

            reservation.Status = ReservationStatus.Confirmada;

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task ReleaseExpiredHoldsAsync(Guid tripId, int holdExpirationMinutes, CancellationToken cancellationToken)
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-holdExpirationMinutes);

            var expiredHolds = await _context.Reservations
                .Where(r => r.TripId == tripId && r.Status == ReservationStatus.PendientePago && r.CreatedAt < cutoff)
                .ToListAsync(cancellationToken);

            if (expiredHolds.Count == 0)
            {

                return;
            }

            foreach (var reservation in expiredHolds)
            {
                reservation.Status = ReservationStatus.Cancelada;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Reservation>> GetAllByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
        {
            return await _context.Reservations
                .Where(r => r.TripId == tripId)
                .ToListAsync(cancellationToken);
        }
    }
}
