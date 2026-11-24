using Checkbus.ApiService.Domain.Authorization;

namespace Checkbus.ApiService.Application.Users.Queries
{
    public class UserListItemDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Surname { get; set; }
        public required string Email { get; set; }
        public required Role Role { get; set; }

    }
}
