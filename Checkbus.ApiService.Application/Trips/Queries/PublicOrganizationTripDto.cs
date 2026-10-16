namespace Checkbus.ApiService.Application.Trips.Queries
{
    // Deliberately thinner than PublicTripDetailDto (odd/tasks/viajes-disponibles-publico.md T1):
    // this is a listing card, not the trip detail step — no TripId, driver, vehicle, or any other
    // field that detail page doesn't need to show here. The anonymous visitor reaches the real
    // detail/reservation flow through TripSlug (-> /reservar/{slug}), same as every other public
    // share link in this codebase, never through a raw Guid.
    public class PublicOrganizationTripDto
    {
        public required string TripSlug { get; set; }
        public required string EventName { get; set; }
        public DateTime DepartureDate { get; set; }
        public decimal Price { get; set; }
    }
}
