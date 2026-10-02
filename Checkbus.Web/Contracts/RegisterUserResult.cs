namespace Checkbus.Web.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.Users.Commands.RegisterUserCommandResult</c>, the 201
/// response body for <c>POST /api/Users</c>. Checkbus.Web does not reference the API projects
/// (pure BFF), so this DTO is duplicated here rather than shared — matching the existing
/// <c>CurrentUserResponse</c> mirror-DTO convention. <c>Role</c> is a plain string on the real
/// result type (it serializes the enum's name, not its number), so it stays a <c>string</c> here
/// too — no enum involved.
/// </summary>
public sealed record RegisterUserResult
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string Name { get; init; }
    public required string Surname { get; init; }
    public required string Role { get; init; }
    public required Guid OrganizationId { get; init; }
    public required bool MustChangePassword { get; init; }
}
