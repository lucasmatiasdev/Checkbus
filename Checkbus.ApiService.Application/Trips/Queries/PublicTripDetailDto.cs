using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    // Step 1 ("detalle del viaje") data for the public reservation page
    // (odd/tasks/detalle-reserva-publica.md T2). Deliberately a public-safe subset — unlike the
    // authenticated TripDetailDto, this NEVER exposes DriverName/VehiclePatent/VehicleBrand/
    // VehicleModel/OwnerUserId or anything else identifying the Vehicle/Driver: an anonymous
    // visitor only needs enough to decide whether to reserve, not operational fleet detail.
    public class PublicTripDetailDto
    {
        public required Guid TripId { get; set; }
        public required string OrganizationName { get; set; }
        public required Guid EventId { get; set; }
        public required string EventName { get; set; }

        // Mirrors EventDto.HasImage (odd/tasks/detalle-reserva-publica.md T1) — never the raw
        // ImageKey. The public page fetches GET api/Events/{EventId}/image directly when true.
        public bool EventHasImage { get; set; }

        public decimal Price { get; set; }

        // Capacity minus the count of existing Reservation rows for this Trip — same computation
        // GetPublicTripSeatMapQueryHandler already does, just without resolving/seeding the
        // individual Seat rows (this endpoint doesn't need per-seat detail, only the count).
        public int AvailableSeats { get; set; }

        public DateTime DepartureDate { get; set; }
        public DateTime ArrivalDate { get; set; }

        public required IReadOnlyList<StopDto> Stops { get; set; }

        public class StopDto
        {
            public int Order { get; set; }
            public StopType Type { get; set; }
            public required string Name { get; set; }
            public required string Address { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }
    }
}
