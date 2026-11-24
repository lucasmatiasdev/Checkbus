using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Application.DriverRequirements.Queries
{

    public class DriverRequirementDto
    {
        public required DriverRequirementType Type { get; set; }
        public required DriverRequirementStatus Status { get; set; }
        public DateOnly? IssueDate { get; set; }
        public DateOnly? ExpirationDate { get; set; }
        public bool DocumentPresent { get; set; }
    }
}
