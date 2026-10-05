namespace Checkbus.Web.Contracts;

/// <summary>
/// Web-side mirror of <c>Checkbus.ApiService.Application.Events.Queries.EventDto</c>, the
/// response body shape for <c>POST /api/Events</c> (201) and one item of the <c>200</c> array
/// for <c>GET /api/Events</c>. Checkbus.Web does not reference the API projects (pure BFF), so
/// this DTO is duplicated here rather than shared.
/// </summary>
public sealed record EventResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required WebEventType Type { get; init; }
    public DateTime Date { get; init; }
    public required LocationResponse Location { get; init; }
}

/// <summary>
/// One item of <see cref="EventResponse.Location"/> — nested rather than flattened, same shape
/// choice as <see cref="VehicleDiagnosticResponse"/>/<see cref="ComponentDiagnosticResponse"/>.
/// </summary>
public sealed record LocationResponse
{
    public required string Name { get; init; }
    public required string Address { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}
