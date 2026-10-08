namespace Checkbus.Web.Features.VehicleDiagnostics.Contracts;

/// <summary>
/// Web-side mirror of one entry of
/// <c>Checkbus.ApiService.Application.VehicleDiagnostics.Commands.ComponentDiagnosticInput</c>,
/// nested inside <see cref="CreateVehicleDiagnosticRequest.Components"/>. These never have an
/// independent lifecycle or endpoint — always submitted together with their parent diagnostic in
/// one request, same as the real server-side command shape.
/// </summary>
public sealed record ComponentDiagnosticInputRequest
{
    public required WebVehicleComponent Component { get; init; }
    public required WebComponentCondition Condition { get; init; }
    public string? Notes { get; init; }
}
