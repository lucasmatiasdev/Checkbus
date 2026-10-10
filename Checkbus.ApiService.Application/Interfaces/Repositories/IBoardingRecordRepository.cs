using Checkbus.ApiService.Domain.Entities.BoardingRecords;
using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IBoardingRecordRepository
    {
        // Lazily, idempotently backfills the BoardingRecord rows a Trip's Reservations never
        // got (mirrors ISeatRepository.EnsureSeatsForVehicleAsync's exact shape — see
        // odd/tasks/reservas-publicas.md Fase 3 analysis). For every Reservation on this Trip
        // with no existing BoardingRecord, inserts one Pendiente row. A no-op read when every
        // row already exists, so it is safe to call on every boarding-screen open.
        Task EnsureForTripAsync(Guid tripId, CancellationToken cancellationToken);

        Task<IReadOnlyList<BoardingRecord>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken);

        Task<BoardingRecord?> GetByReservationIdAsync(Guid reservationId, CancellationToken cancellationToken);

        // Finds the BoardingRecord for reservationId and moves it to Abordado, recording
        // method and ValidatedAt = DateTime.UtcNow. Throws BoardingRecordNotFoundException if
        // no row resolves, BoardingAlreadyValidatedException if the row is no longer
        // Pendiente (already Abordado or Ausente) — no re-validating an already-resolved
        // record.
        Task ValidateAsync(Guid reservationId, ValidationMethod method, CancellationToken cancellationToken);

        // Bulk Pendiente -> Ausente for every BoardingRecord belonging to a Reservation on
        // this Trip — the "Cerrar embarque" action. Rows already Abordado are left untouched.
        Task CloseBoardingAsync(Guid tripId, CancellationToken cancellationToken);
    }
}
