namespace Checkbus.Web.Features.Trips.Contracts;

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
