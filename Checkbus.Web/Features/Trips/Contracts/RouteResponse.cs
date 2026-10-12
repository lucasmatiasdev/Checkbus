namespace Checkbus.Web.Features.Trips.Contracts;

/// <summary>One <see cref="TripDetailResponse.Route"/> — its computed estimates plus ordered stops.</summary>
public sealed record RouteResponse
{
    public TimeSpan EstimatedDuration { get; init; }
    public decimal DistanceKm { get; init; }
    public required IReadOnlyList<StopResponse> Stops { get; init; }
}
