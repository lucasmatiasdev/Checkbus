using MediatR;

namespace Checkbus.ApiService.Application.Users.Queries
{

    public class GetUsersByOrganizationQuery : IRequest<IReadOnlyList<UserListItemDto>>
    {
        public required Guid OrganizationId { get; set; }
    }
}
