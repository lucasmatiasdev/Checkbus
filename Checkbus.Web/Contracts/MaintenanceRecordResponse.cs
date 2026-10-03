namespace Checkbus.Web.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.MaintenanceRecords.Queries.MaintenanceRecordDto</c>, the
/// response body shape for <c>POST /api/MaintenanceRecords</c> (201), and one item of the
/// <c>200</c> array for <c>GET /api/MaintenanceRecords</c> / the <c>200</c> body for
/// <c>GET /api/MaintenanceRecords/{id}</c>. Checkbus.Web does not reference the API projects (pure
/// BFF), so this DTO is duplicated here rather than shared. <see cref="VehiclePatent"/>,
/// <see cref="VehicleBrand"/>, and <see cref="VehicleModel"/> are display-only join fields from the
/// owning vehicle, present so the standalone (non-vehicle-nested) maintenance UI can render a
/// list/detail without a second round trip.
/// </summary>
public sealed record MaintenanceRecordResponse
{
    public required Guid Id { get; init; }
    public required Guid VehicleId { get; init; }
    public required WebMaintenanceType Type { get; init; }
    public DateOnly ScheduledDate { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public WebMaintenanceStatus Status { get; init; }
    public required string Description { get; init; }
    public string? MechanicNotes { get; init; }
    public decimal? Cost { get; init; }
    public string? VehiclePatent { get; init; }
    public string? VehicleBrand { get; init; }
    public string? VehicleModel { get; init; }
}
