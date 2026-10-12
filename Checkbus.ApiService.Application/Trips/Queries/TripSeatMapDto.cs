namespace Checkbus.ApiService.Application.Trips.Queries
{
    // Authenticated counterpart to PublicTripSeatMapDto — same Capacity/Price/
    // OccupiedSeatNumbers shape, visual occupied/free ONLY. Deliberately does NOT include
    // PassengerId, a Seat->SeatId mapping, or any passenger field at all (confirmed decision,
    // not just hidden in the UI): the Planificador's panel answers exactly "qué asientos se
    // ocuparon", nothing more.
    public class TripSeatMapDto
    {
        public required Guid TripId { get; set; }
        public int Capacity { get; set; }
        public decimal Price { get; set; }
        public required IReadOnlyList<int> OccupiedSeatNumbers { get; set; }
    }
}
