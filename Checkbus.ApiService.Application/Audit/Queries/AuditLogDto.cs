using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Application.Audit.Queries
{
    // CU-16 flow step 15 fields: fecha, usuario, acción, entidad afectada, descripción.
    // OrganizationName is only populated when the handler resolved it (CheckbusSoporte viewing
    // a cross-tenant scope) — null for an Administrador viewing their own single organization,
    // same "only resolve what the screen needs" idea as SupportTicketAdminDto.OrganizationName
    // but nullable here because this DTO is shared by both roles' scopes.
    public class AuditLogDto
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public required string ActorEmail { get; set; }
        public AuditAction Action { get; set; }
        public required string EntityType { get; set; }
        public Guid EntityId { get; set; }
        public required string Description { get; set; }
        public string? OrganizationName { get; set; }
    }
}
