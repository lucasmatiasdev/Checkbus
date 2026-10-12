using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.BoardingRecords;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Boarding;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Implementations.Repositories
{
    public class BoardingRecordRepository : IBoardingRecordRepository
    {
        private readonly CheckbusDbContext _context;

        public BoardingRecordRepository(CheckbusDbContext context)
        {
            _context = context;
        }

        public async Task EnsureForTripAsync(Guid tripId, CancellationToken cancellationToken)
        {
            var reservationIds = await _context.Reservations
                .Where(r => r.TripId == tripId)
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);

            if (reservationIds.Count == 0)
            {
                return;
            }

            var existingReservationIds = await _context.BoardingRecords
                .Where(b => reservationIds.Contains(b.ReservationId))
                .Select(b => b.ReservationId)
                .ToListAsync(cancellationToken);

            if (existingReservationIds.Count >= reservationIds.Count)
            {
                // Idempotent no-op read — every Reservation on this Trip already has a
                // BoardingRecord.
                return;
            }

            var existingSet = existingReservationIds.ToHashSet();
            var missingRecords = reservationIds
                .Where(id => !existingSet.Contains(id))
                .Select(reservationId => new BoardingRecord { Id = Guid.NewGuid(), ReservationId = reservationId });

            _context.BoardingRecords.AddRange(missingRecords);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<BoardingRecord>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
        {
            var reservationIds = await _context.Reservations
                .Where(r => r.TripId == tripId)
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);

            if (reservationIds.Count == 0)
            {
                return [];
            }

            return await _context.BoardingRecords
                .Where(b => reservationIds.Contains(b.ReservationId))
                .ToListAsync(cancellationToken);
        }

        public Task<BoardingRecord?> GetByReservationIdAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            return _context.BoardingRecords.FirstOrDefaultAsync(b => b.ReservationId == reservationId, cancellationToken);
        }

        public async Task ValidateAsync(Guid reservationId, ValidationMethod method, CancellationToken cancellationToken)
        {
            var record = await _context.BoardingRecords
                .FirstOrDefaultAsync(b => b.ReservationId == reservationId, cancellationToken)
                ?? throw new BoardingRecordNotFoundException();

            if (record.Status != BoardingStatus.Pendiente)
            {
                throw new BoardingAlreadyValidatedException();
            }

            record.Status = BoardingStatus.Abordado;
            record.Method = method;
            record.ValidatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task CloseBoardingAsync(Guid tripId, CancellationToken cancellationToken)
        {
            var reservationIds = await _context.Reservations
                .Where(r => r.TripId == tripId)
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);

            if (reservationIds.Count == 0)
            {
                return;
            }

            var pendingRecords = await _context.BoardingRecords
                .Where(b => reservationIds.Contains(b.ReservationId) && b.Status == BoardingStatus.Pendiente)
                .ToListAsync(cancellationToken);

            foreach (var record in pendingRecords)
            {
                record.Status = BoardingStatus.Ausente;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
