using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Domain.Entities.Reservations
{
    // One row per person, not per purchase — a family of 3 booking together on one Trip
    // still produces 3 Reservation rows, one per Seat. FK-only, no navigation properties
    // back to Trip/Seat/Passenger, same pattern as VehicleDocument -> Vehicle. Price is
    // always a copy of Trip.Price taken at creation time (no per-seat pricing exists).
    // Status only has Confirmada in Fase 1 — a Reservation is confirmed immediately on
    // creation, there is no payment step; Cancelada arrives with Fase 4.
    public class Reservation
    {
        public Guid Id { get; set; }
        public required Guid TripId { get; set; }
        public required Guid SeatId { get; set; }
        public required Guid PassengerId { get; set; }
        public required decimal Price { get; set; }
        public ReservationStatus Status { get; set; } = ReservationStatus.Confirmada;
    }
}
