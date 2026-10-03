namespace Checkbus.Web.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.VehicleDiagnostics.Queries.ComponentDiagnosticDto</c>, one
/// item of <see cref="VehicleDiagnosticResponse.Components"/>. Checkbus.Web does not reference the
/// API projects (pure BFF), so this DTO is duplicated here rather than shared.
/// </summary>
public sealed record ComponentDiagnosticResponse
{
    public required Guid Id { get; init; }
    public required WebVehicleComponent Component { get; init; }
    public required WebComponentCondition Condition { get; init; }
    public string? Notes { get; init; }
}
