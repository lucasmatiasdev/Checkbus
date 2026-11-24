namespace Checkbus.ApiService.Application.Trips.Queries
{

    public class PublicTripSeatMapDto
    {
        public required Guid TripId { get; set; }
        public int Capacity { get; set; }
        public decimal Price { get; set; }
        public required IReadOnlyList<int> OccupiedSeatNumbers { get; set; }

        public required IReadOnlyList<SeatMapSeatDto> Seats { get; set; }
    }

    public class SeatMapSeatDto
    {
        public required Guid SeatId { get; set; }
        public required int Number { get; set; }
    }
}
