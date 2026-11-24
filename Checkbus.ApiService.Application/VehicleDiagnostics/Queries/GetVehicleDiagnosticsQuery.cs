using MediatR;

namespace Checkbus.ApiService.Application.VehicleDiagnostics.Queries
{

    public class GetVehicleDiagnosticsQuery : IRequest<IReadOnlyList<VehicleDiagnosticDto>>
    {
        public required Guid MaintenanceRecordId { get; set; }
    }
}
