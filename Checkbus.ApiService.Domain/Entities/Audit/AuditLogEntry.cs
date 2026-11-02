using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Domain.Entities.Audit
{
    // CU-16 (odd/tasks/auditoria-sistema.md) — append-only audit trail for a curated list of
    // sensitive actions, written automatically by the business handler that performs the action,
    // never by a manual action of this use case. No before/after snapshot of the affected
    // entity's values, only that the action happened, who did it, on what, and when (CU-16
    // postcondición).
    //
    // OrganizationId/ActorUserId are nullable: a failed login attempt with an email that matches
    // no User has no Organization/User row to attribute — only the attempted ActorEmail survives
    // for that case.
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
