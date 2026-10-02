using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Domain.Entities.Documents
{
    // One row per (Chofer, Type) pair, enforced by a unique index on (UserId, Type) in
    // CheckbusDbContext. Deliberately no navigation property back to User — the
    // relationship stays FK-only to avoid widening User's surface for this feature.
    // Vigencia (expired/expiring) is never stored here: only ExpirationDate is persisted,
    // and the "about to expire or expired" determination happens at query/render time.
    public class DriverRequirement
    {
        public Guid Id { get; set; }
        public required Guid UserId { get; set; }
        public required DriverRequirementType Type { get; set; }
        public DriverRequirementStatus Status { get; set; } = DriverRequirementStatus.Pendiente;
        public DateOnly? IssueDate { get; set; }
        public DateOnly? ExpirationDate { get; set; }
        public bool DocumentPresent { get; set; }
        public string? FileKey { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
