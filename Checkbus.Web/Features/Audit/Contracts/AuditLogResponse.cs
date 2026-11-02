namespace Checkbus.Web.Features.Audit.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.Audit.Queries.AuditLogDto</c>, one item of the <c>200</c>
/// response body array for <c>GET /api/audit-log</c> (CU-16, <c>odd/tasks/auditoria-sistema.md</c>).
/// Checkbus.Web does not reference the API projects (pure BFF), so this DTO is duplicated here
/// rather than shared. <see cref="OrganizationName"/> is only populated when the caller is
/// CheckbusSoporte viewing a cross-tenant scope — null for an Administrador viewing their own
/// single organization, same nullability as the real DTO.
/// </summary>
public sealed record AuditLogResponse
{
    public required Guid Id { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required string ActorEmail { get; init; }
    public required WebAuditAction Action { get; init; }
    public required string EntityType { get; init; }
    public required Guid EntityId { get; init; }
    public required string Description { get; init; }
    public string? OrganizationName { get; init; }
}
