namespace Checkbus.ApiService.Application.Trips.Queries
{
    // Response shape for the project's first business-domain anonymous surface
    // (odd/tasks/reservas-publicas.md R3). Deliberately minimal: the public page only needs
    // Capacity to compute the 2+2-with-aisle grid, Price to display per-seat cost, and which
    // Seat.Numbers are already taken — never full Seat/Reservation/Passenger objects, which
    // would leak more than an anonymous visitor needs.
    public class PublicTripSeatMapDto
    {
        public required Guid TripId { get; set; }
        public int Capacity { get; set; }
        public decimal Price { get; set; }
        public required IReadOnlyList<int> OccupiedSeatNumbers { get; set; }

        // Added for R4 (odd/tasks/reservas-publicas.md): the reservation-create request needs
        // a real SeatId per selected seat, not just its display Number — this is every Seat
        // row for the trip's vehicle (free and occupied alike), so the public page can resolve
        // "seat number the visitor clicked" -> "SeatId to submit" without a second round trip.
        // Exposing a Seat's own id is not a privacy concern the way exposing Passenger/
        // Reservation data would be.
        public required IReadOnlyList<SeatMapSeatDto> Seats { get; set; }
    }

    public class SeatMapSeatDto
    {
        public required Guid SeatId { get; set; }
        public required int Number { get; set; }
    }
}
