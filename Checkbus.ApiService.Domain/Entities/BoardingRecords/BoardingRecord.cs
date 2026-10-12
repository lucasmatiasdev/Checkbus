using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Domain.Entities.BoardingRecords
{
    // One row per Reservation on a Trip, lazily and idempotently created the first time a
    // Chofer opens that Trip's boarding screen (IBoardingRecordRepository.EnsureForTripAsync
    // mirrors ISeatRepository.EnsureSeatsForVehicleAsync's exact shape — see
    // odd/tasks/reservas-publicas.md Fase 3 analysis). FK-only, no navigation property back to
    // Reservation, same pattern as every other entity here. ReservationId is unique: this is a
    // true 1:1 with Reservation, enforced by a unique index in CheckbusDbContext. Method stays
    // null until the row is actually validated.
    public class BoardingRecord
    {
        public Guid Id { get; set; }
        public required Guid ReservationId { get; set; }
        public BoardingStatus Status { get; set; } = BoardingStatus.Pendiente;
        public ValidationMethod? Method { get; set; }
        public DateTime? ValidatedAt { get; set; }
    }
}
