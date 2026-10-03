namespace Checkbus.Web.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.MaintenanceRecords.Commands.CreateMaintenanceRecordCommand</c>,
/// the request body for <c>POST /api/MaintenanceRecords</c>. Checkbus.Web does not reference the
/// API projects (pure BFF), so this DTO is duplicated here rather than shared. <c>OrganizationId</c>
/// is deliberately absent — tenant scoping is resolved exclusively from the authenticated caller's
/// token server-side, same pattern as <see cref="CreateVehicleRequest"/>.
/// </summary>
public sealed record CreateMaintenanceRecordRequest
{
    public required Guid VehicleId { get; init; }
    public required WebMaintenanceType Type { get; init; }
    public required DateOnly ScheduledDate { get; init; }
    public required string Description { get; init; }
}
