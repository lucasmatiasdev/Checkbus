namespace Checkbus.Web.Contracts;

/// <summary>
/// Web-side mirror of the colocated
/// <c>Checkbus.ApiService.Contracts.UpdateMaintenanceRecordRequest</c> record declared in
/// <c>MaintenanceRecordsController.cs</c>, the request body for
/// <c>PUT /api/MaintenanceRecords/{id}</c>. Checkbus.Web does not reference the API projects (pure
/// BFF), so this DTO is duplicated here rather than shared. <c>Id</c> is deliberately absent — it
/// travels as the route parameter, not the body, matching the real server-side contract.
/// </summary>
public sealed record UpdateMaintenanceRecordRequest
{
    public required WebMaintenanceStatus Status { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string? MechanicNotes { get; init; }
    public decimal? Cost { get; init; }
}
