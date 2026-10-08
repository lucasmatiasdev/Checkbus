using Checkbus.Web.Features.VehicleDiagnostics.Contracts;

namespace Checkbus.Web.Features.Events.Contracts;

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
