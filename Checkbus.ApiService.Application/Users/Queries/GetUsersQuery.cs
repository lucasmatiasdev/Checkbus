using MediatR;

namespace Checkbus.ApiService.Application.Users.Queries
{
    public class GetUsersQuery : IRequest<IReadOnlyList<UserListItemDto>>
    {

    }
}
