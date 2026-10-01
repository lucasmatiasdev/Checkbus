namespace Checkbus.ApiService.Application.Users.Commands
{
    public class RegisterUserCommandResult
    {
        public required Guid UserId { get; set; }
        public required string Email { get; set; }
        public required string Name { get; set; }
        public required string Surname { get; set; }
        public required string Role { get; set; }
        public required Guid OrganizationId { get; set; }
        public required bool MustChangePassword { get; set; }
    }
}
