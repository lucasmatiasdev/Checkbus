namespace Checkbus.ApiService.Application.Tenancy.Commands
{
    /// <summary>
    /// Shape the CheckbusSoporte UI needs right after <see cref="RegisterOrganizationCommand"/>
    /// succeeds, to show "organization created — admin login is AdminEmail" the same way
    /// RegisterUser.razor's UserCreatedDialog already does for plain user registration. The
    /// initial password itself is deliberately NOT echoed back here: it's always
    /// RegisterOrganizationCommand.AdminDocumentNumber, a value the calling operator already
    /// typed into the same request, so the UI can display it from its own form state — same
    /// reasoning RegisterUserCommandResult already applies (it has no password field either).
    /// </summary>
    public class RegisterOrganizationCommandResult
    {
        public required Guid OrganizationId { get; set; }
        public required string OrganizationName { get; set; }
        public required string Slug { get; set; }
        public required Guid AdminUserId { get; set; }
        public required string AdminEmail { get; set; }
        public required string AdminName { get; set; }
        public required string AdminSurname { get; set; }
    }
}
