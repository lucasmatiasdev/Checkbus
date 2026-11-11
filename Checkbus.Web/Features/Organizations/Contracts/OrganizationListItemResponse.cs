namespace Checkbus.Web.Features.Organizations.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.Tenancy.Queries.OrganizationListItemDto</c>, one item of the
/// <c>200</c> response body array for <c>GET /api/organizations</c> (CheckbusSoporte-only; CU-16's
/// [Organización] dropdown, <c>odd/tasks/auditoria-sistema.md</c> T4). Checkbus.Web does not
/// reference the API projects (pure BFF), so this DTO is duplicated here rather than shared.
/// </summary>
public sealed record OrganizationListItemResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
}
