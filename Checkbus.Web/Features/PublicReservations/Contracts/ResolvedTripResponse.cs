namespace Checkbus.Web.Features.PublicReservations.Contracts;

/// <summary>Response body of <c>GET /api/public/trips/by-slug/{slug}</c>.</summary>
public sealed record ResolvedTripResponse
{
    public required Guid TripId { get; init; }
}
