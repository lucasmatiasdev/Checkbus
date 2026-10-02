using MediatR;

namespace Checkbus.ApiService.Application.Users.Queries
{
    public class GetUsersQuery : IRequest<IReadOnlyList<UserListItemDto>>
    {
        // Deliberately ABSENT: OrganizationId — tenant scoping is resolved exclusively
        // from the authenticated caller's token inside the handler, mirroring
        // RegisterUserCommand's anti-IDOR rationale (a body/query-supplied value would
        // be a cross-tenant IDOR).
    }
}
