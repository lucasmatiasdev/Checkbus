using MediatR;

namespace Checkbus.ApiService.Application.Users.Queries
{
    // CU-16 (odd/tasks/auditoria-sistema.md T4) — populates the [Usuario] dropdown for a
    // CheckbusSoporte caller after it picks an Organization (step 9 of the main flow).
    // Deliberately a SEPARATE query from GetUsersQuery rather than adding an OrganizationId
    // there: GetUsersQuery documents on purpose that tenant scoping only ever comes from the
    // caller's own claim (anti-IDOR), and this query exists only for the CheckbusSoporte-gated
    // cross-tenant read, never reachable by Administrador/Planificador.
    public class GetUsersByOrganizationQuery : IRequest<IReadOnlyList<UserListItemDto>>
    {
        public required Guid OrganizationId { get; set; }
    }
}
