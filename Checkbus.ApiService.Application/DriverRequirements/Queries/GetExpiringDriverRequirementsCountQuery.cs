using MediatR;

namespace Checkbus.ApiService.Application.DriverRequirements.Queries
{
    public class GetExpiringDriverRequirementsCountQuery : IRequest<int>
    {
        // Deliberately ABSENT: OrganizationId — scoped exclusively to the authenticated
        // caller's own organization inside the handler, same anti-IDOR rationale as
        // GetUsersQuery. No per-target check is needed: this is an aggregate over the
        // caller's whole organization, not a lookup on a specific user.
    }
}
