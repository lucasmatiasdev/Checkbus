using Checkbus.Web.Features.Users.Contracts;

namespace Checkbus.Web.Features.Organizations.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.Tenancy.Commands.RegisterOrganizationCommand</c>, the
/// request body for <c>POST /api/Organizations</c>. Checkbus.Web does not reference the API
/// projects (pure BFF), so this DTO is duplicated here rather than shared — matching the existing
/// <c>RegisterUserRequest</c> mirror-DTO convention. Reuses <see cref="WebDocumentType"/> from the
/// Users feature rather than duplicating it again — that enum is already shared across
/// PublicReservations and Boarding the same way.
/// </summary>
public sealed record RegisterOrganizationRequest
{
    public required string OrganizationName { get; init; }
    public required string CUIT { get; init; }
    public required string AdminName { get; init; }
    public required string AdminSurname { get; init; }
    public WebDocumentType AdminDocumentType { get; init; } = WebDocumentType.DNI;
    public required string AdminDocumentNumber { get; init; }
}
