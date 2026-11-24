using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Application.Audit.Queries
{

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
