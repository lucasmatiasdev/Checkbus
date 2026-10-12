namespace Checkbus.Web.Features.Trips.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.Trips.Queries.TripSeatMapDto</c>, the response body for
/// <c>GET api/Trips/{id}/seats</c> (odd/tasks/mejoras-detalle-viaje.md Fase E1). Visual
/// occupied/free only — deliberately no passenger field, mirroring the DTO's own confirmed
/// decision.
/// </summary>
public sealed record TripSeatMapResponse
{
    public required Guid TripId { get; init; }
    public int Capacity { get; init; }
    public decimal Price { get; init; }
    public required IReadOnlyList<int> OccupiedSeatNumbers { get; init; }
}
