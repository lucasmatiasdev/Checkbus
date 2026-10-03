using MediatR;

namespace Checkbus.ApiService.Application.VehicleDocuments.Queries
{
    public class GetExpiringVehicleDocumentsCountQuery : IRequest<int>
    {
        // Deliberately ABSENT: OrganizationId — scoped exclusively to the authenticated
        // caller's own organization inside the handler, same anti-IDOR rationale as
        // GetExpiringDriverRequirementsCountQuery. Independent from that driver-requirements
        // count by design — this feature's dashboard card shows its own separate number.
    }
}
