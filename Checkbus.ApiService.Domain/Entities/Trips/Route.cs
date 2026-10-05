namespace Checkbus.ApiService.Domain.Entities.Trips
{
    // Composition, not reusable: a Route never exists without exactly one Trip and is
    // never shared. TripId is unique, mirroring the existing child-holds-FK-to-parent
    // convention (FK lives on the child, no navigation property back).
    // EstimatedDuration/DistanceKm are calculated once, server-side, at Trip creation time
    // via the Directions API — never recalculated on read, never trusted from the client.
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
