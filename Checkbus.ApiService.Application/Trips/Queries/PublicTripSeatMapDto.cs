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
    }
}
