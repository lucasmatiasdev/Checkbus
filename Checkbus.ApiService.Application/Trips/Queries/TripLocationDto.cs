namespace Checkbus.ApiService.Application.Trips.Queries
{
    public class TripLocationDto
    {
        public bool LiveTrackingEnabled { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
