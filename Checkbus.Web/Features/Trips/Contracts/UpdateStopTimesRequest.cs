namespace Checkbus.Web.Features.Trips.Contracts;

/// <summary>
/// Web-side mirror of the API's <c>Checkbus.ApiService.Contracts.UpdateStopTimesRequest</c>,
/// the request body for <c>PUT api/Trips/{id}/stops/times</c>
/// (odd/tasks/mejoras-detalle-viaje.md Fase D3).
/// </summary>
public sealed record UpdateStopTimesRequest
{
    public required IReadOnlyList<StopDepartureTimeRequest> Stops { get; init; }
}

public sealed record StopDepartureTimeRequest
{
    public required Guid StopId { get; init; }
    public required DateTime DepartureTime { get; init; }
}
