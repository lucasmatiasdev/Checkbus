using MediatR;

namespace Checkbus.ApiService.Application.Tenancy.Queries
{

    public class GetOrganizationsQuery : IRequest<IReadOnlyList<OrganizationListItemDto>>
    {
    }
}
