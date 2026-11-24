using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Domain.Entities.Audit
{

    public class AuditLogEntry
    {
        public Guid Id { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? ActorUserId { get; set; }
        public required string ActorEmail { get; set; }
        public AuditAction Action { get; set; }
        public required string EntityType { get; set; }
        public Guid EntityId { get; set; }
        public required string Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
