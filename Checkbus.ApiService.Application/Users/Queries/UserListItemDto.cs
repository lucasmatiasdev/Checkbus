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

        // Deliberately ABSENT: PasswordHash and any other sensitive User field —
        // this DTO is what crosses the HTTP boundary to the caller.
    }
}
