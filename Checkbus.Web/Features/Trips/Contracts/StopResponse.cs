namespace Checkbus.Web.Features.Trips.Contracts;

/// <summary>One ordered stop of <see cref="RouteResponse.Stops"/>, with its resolved Location.</summary>
public sealed record StopResponse
{

    public Guid Id { get; init; }

    public int Order { get; init; }
    public required WebStopType Type { get; init; }
    public required string Name { get; init; }
    public required string Address { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }

    public DateTime? DepartureTime { get; init; }
}
