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

        // Nullable for backward compatibility with Stop rows created before this field existed
        // (this project uses EnsureCreatedAsync(), not migrations, so a non-nullable new column
        // could not safely backfill an already-existing table). Every NEWLY created Stop going
        // forward always gets a real value — set at Trip creation (CreateTripCommandHandler)
        // and editable afterward (UpdateStopTimesCommandHandler). UTC, same Kind convention as
        // Trip.DepartureDate.
        public DateTime? DepartureTime { get; set; }

        // CU-13: how long the vehicle idles at this stop, caller-provided at Trip creation
        // (CreateTripCommandHandler) — 0 means no extra stop time, not "unspecified".
        public int StopDurationMinutes { get; set; }

        // CU-13: whether passengers may board/alight at this stop — distinct from StopType
        // (Origen/Intermedia/Destino is the stop's position in the route; this is a caller
        // decision about whether it's actually usable for boarding, e.g. a fuel/rest stop with
        // no boarding).
        public bool IsBoardingPoint { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
