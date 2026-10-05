using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Domain.Entities.Trips
{
    // A Location's role within one specific Route. FK-only, no navigation properties back
    // to Route/Location — same pattern as VehicleDocument -> Vehicle. No independent
    // lifecycle or endpoint: always created together with its parent Trip/Route.
    public class Stop
    {
        public Guid Id { get; set; }
        public required Guid RouteId { get; set; }
        public required Guid LocationId { get; set; }
        public int Order { get; set; }
        public StopType Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
