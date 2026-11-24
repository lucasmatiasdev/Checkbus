namespace Checkbus.ApiService.Application.Trips.Queries
{

    public class PublicOrganizationTripDto
    {
        public required string TripSlug { get; set; }
        public required string EventName { get; set; }
        public DateTime DepartureDate { get; set; }
        public decimal Price { get; set; }
    }
}
