using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Application.DriverRequirements.Queries
{
    // Deliberately EXCLUDES FileKey/FileContentType: the uploaded file is only ever
    // reachable through the dedicated download endpoint (GetDriverRequirementDocumentQuery),
    // never as a raw storage key handed to the client.
    public class DriverRequirementDto
    {
        public required DriverRequirementType Type { get; set; }
        public required DriverRequirementStatus Status { get; set; }
        public DateOnly? IssueDate { get; set; }
        public DateOnly? ExpirationDate { get; set; }
        public bool DocumentPresent { get; set; }
    }
}
