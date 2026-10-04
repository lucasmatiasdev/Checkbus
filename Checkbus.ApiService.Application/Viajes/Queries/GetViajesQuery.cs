using MediatR;

namespace Checkbus.ApiService.Application.Viajes.Queries
{
    // Org-scoped — the organization comes exclusively from the validated caller claim, mirroring
    // GetVehiclesQuery/GetMaintenanceRecordsQuery. No request fields at all.
    public class GetViajesQuery : IRequest<IReadOnlyList<ViajeDto>>
    {
    }
}
