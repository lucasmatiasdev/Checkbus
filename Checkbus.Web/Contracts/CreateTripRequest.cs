namespace Checkbus.Web.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.Trips.Commands.CreateTripCommand</c>, the request body
/// for <c>POST /api/Trips</c>. Each stop's location fields are flattened to match
/// <c>Checkbus.Web.Models.PickedLocation</c>'s shape exactly (same convention as
/// <c>CreateEventRequest</c>), so <c>TripNew.razor</c> can map R3's <c>LocationPicker</c>
/// <c>MultiStop</c> output straight into the ordered <see cref="Stops"/> list with no renaming.
/// </summary>
public sealed record CreateTripRequest
{
    public required Guid VehicleId { get; init; }
    public required Guid DriverId { get; init; }
    public required Guid EventId { get; init; }
    public required DateTime DepartureDate { get; init; }
    public required DateTime ArrivalDate { get; init; }
    public required decimal Price { get; init; }
    public required IReadOnlyList<CreateTripStopRequest> Stops { get; init; }
}

/// <summary>
/// One ordered stop of <see cref="CreateTripRequest.Stops"/>. Order/Type are deliberately
/// absent — both are computed server-side from this list's position.
/// </summary>
public sealed record CreateTripStopRequest
{
    public required string Name { get; init; }
    public required string Address { get; init; }
    public required string PlaceId { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
}
