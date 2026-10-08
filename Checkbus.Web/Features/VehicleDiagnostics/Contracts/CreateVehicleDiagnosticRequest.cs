namespace Checkbus.Web.Features.VehicleDiagnostics.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.VehicleDiagnostics.Commands.CreateVehicleDiagnosticCommand</c>,
/// the request body for <c>POST /api/VehicleDiagnostics</c>. Checkbus.Web does not reference the
/// API projects (pure BFF), so this DTO is duplicated here rather than shared. <c>VehicleId</c> and
/// <c>OrganizationId</c> are deliberately absent — resolved server-side exclusively from the parent
/// <c>MaintenanceRecord</c> and the authenticated caller's token, same pattern as the real command.
/// </summary>
public sealed record CreateVehicleDiagnosticRequest
{
    public required Guid MaintenanceRecordId { get; init; }
    public required DateOnly DiagnosedAt { get; init; }
    public string? Notes { get; init; }
    public required List<ComponentDiagnosticInputRequest> Components { get; init; }
}
