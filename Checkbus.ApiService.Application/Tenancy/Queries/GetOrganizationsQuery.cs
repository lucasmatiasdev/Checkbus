using MediatR;

namespace Checkbus.ApiService.Application.Tenancy.Queries
{
    // CU-16 (odd/tasks/auditoria-sistema.md T4) — populates the [Organización] dropdown for a
    // CheckbusSoporte caller (step 8 of the main flow). No filters: every registered
    // Organization, same scope as GetAllSupportTicketsQueryHandler's cross-tenant read.
    public class GetOrganizationsQuery : IRequest<IReadOnlyList<OrganizationListItemDto>>
    {
    }
}
