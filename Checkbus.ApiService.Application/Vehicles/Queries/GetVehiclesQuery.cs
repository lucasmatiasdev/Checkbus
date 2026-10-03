using MediatR;

namespace Checkbus.ApiService.Application.Vehicles.Queries
{
    public class GetVehiclesQuery : IRequest<IReadOnlyList<VehicleListItemDto>>
    {
        // Deliberately ABSENT: OrganizationId — tenant scoping is resolved exclusively
        // from the authenticated caller's token inside the handler, mirroring
        // GetUsersQuery's anti-IDOR rationale (a body/query-supplied value would be a
        // cross-tenant IDOR).
    }
}
