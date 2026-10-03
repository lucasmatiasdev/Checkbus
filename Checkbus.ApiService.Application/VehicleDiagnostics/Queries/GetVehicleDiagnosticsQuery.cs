using MediatR;

namespace Checkbus.ApiService.Application.VehicleDiagnostics.Queries
{
    // Lists VehicleDiagnostic entries (with their nested ComponentDiagnostic rows) for a given
    // MaintenanceRecord. MaintenanceRecordId is required — unlike GetMaintenanceRecordsQuery's
    // optional filters, a diagnostics list always belongs to exactly one maintenance record.
    public class GetVehicleDiagnosticsQuery : IRequest<IReadOnlyList<VehicleDiagnosticDto>>
    {
        public required Guid MaintenanceRecordId { get; set; }
    }
}
