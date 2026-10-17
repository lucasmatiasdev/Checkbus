namespace Checkbus.Web.Features.Organizations.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.Tenancy.Commands.RegisterOrganizationCommandResult</c>, the
/// 201 response body for <c>POST /api/Organizations</c>. Checkbus.Web does not reference the API
/// projects (pure BFF), so this DTO is duplicated here rather than shared — matching the existing
/// <c>RegisterUserResult</c> mirror-DTO convention. The initial password is deliberately NOT part
/// of this shape — same reasoning as the real result type: it's always
/// <c>RegisterOrganizationFormModel.AdminDocumentNumber</c>, a value the calling operator already
/// typed into the same request, so the page can read it back from its own form state.
/// </summary>
public sealed record RegisterOrganizationResult
{
    public required Guid OrganizationId { get; init; }
    public required string OrganizationName { get; init; }
    public required string Slug { get; init; }
    public required Guid AdminUserId { get; init; }
    public required string AdminEmail { get; init; }
    public required string AdminName { get; init; }
    public required string AdminSurname { get; init; }
}
