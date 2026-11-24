namespace Checkbus.ApiService.Domain.Entities.Trips
{

    public class Route
    {
        public Guid Id { get; set; }
        public required Guid TripId { get; set; }
        public TimeSpan EstimatedDuration { get; set; }
        public decimal DistanceKm { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
