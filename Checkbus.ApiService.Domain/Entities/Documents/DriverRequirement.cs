using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Domain.Entities.Documents
{

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

        public string? FileContentType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
