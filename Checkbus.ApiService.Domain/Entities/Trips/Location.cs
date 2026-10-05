namespace Checkbus.ApiService.Domain.Entities.Trips
{
    // Shared catalog of real-world places — deliberately no OrganizationId, the same
    // physical place can be reused across organizations/routes/events. PlaceId is
    // Google's place id and is used to dedupe: callers must look up by PlaceId before
    // inserting a new row (see ILocationRepository.FindByPlaceIdAsync).
    public class Location
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Address { get; set; }
        public required string PlaceId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
