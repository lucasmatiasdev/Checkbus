namespace Checkbus.Web.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.VehicleDiagnostics.Queries.VehicleDiagnosticDto</c>, the
/// response body shape for <c>POST /api/VehicleDiagnostics</c> (201), and one item of the
/// <c>200</c> array for <c>GET /api/VehicleDiagnostics?maintenanceRecordId=...</c>. Checkbus.Web
/// does not reference the API projects (pure BFF), so this DTO is duplicated here rather than
/// shared.
/// </summary>
public sealed record VehicleDiagnosticResponse
{
    public required Guid Id { get; init; }
    public required Guid VehicleId { get; init; }
    public required Guid MaintenanceRecordId { get; init; }
    public DateOnly DiagnosedAt { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<ComponentDiagnosticResponse> Components { get; init; } = [];
}
