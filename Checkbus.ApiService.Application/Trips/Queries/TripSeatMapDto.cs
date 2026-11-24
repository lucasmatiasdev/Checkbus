namespace Checkbus.ApiService.Application.Trips.Queries
{

    public class TripSeatMapDto
    {
        public required Guid TripId { get; set; }
        public int Capacity { get; set; }
        public decimal Price { get; set; }
        public required IReadOnlyList<int> OccupiedSeatNumbers { get; set; }
    }
}
