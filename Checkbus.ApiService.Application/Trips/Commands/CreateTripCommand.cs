using Checkbus.ApiService.Application.Trips.Queries;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    public class CreateTripCommand : IRequest<TripDto>
    {
        public required Guid VehicleId { get; set; }
        public required Guid DriverId { get; set; }
        public required Guid EventId { get; set; }
        public required DateTime DepartureDate { get; set; }
        public required DateTime ArrivalDate { get; set; }
        public required decimal Price { get; set; }

        // Ordered: first = origin, last = destination, everything else = intermediate. Order/Type
        // are deliberately absent from CreateTripStopCommand below — both are computed
        // server-side from this list's position, never accepted from the client.
        public required IReadOnlyList<CreateTripStopCommand> Stops { get; set; }

        // Deliberately ABSENT: OrganizationId (from the validated token, like CreateVehicleCommand)
        // and EstimatedDuration/DistanceKm (always computed server-side via IDirectionsService,
        // never trusted from the client).
    }

    // Raw picked-location fields for one ordered stop, crossing the wire from Checkbus.Web's
    // PickedLocation (see R3) — the API-side equivalent of CreateEventCommand's flattened
    // Location* fields. The handler resolves/dedupes the real Location row from these.
    public class CreateTripStopCommand
    {
        public required string Name { get; set; }
        public required string Address { get; set; }
        public required string PlaceId { get; set; }
        public required double Latitude { get; set; }
        public required double Longitude { get; set; }
    }
}
