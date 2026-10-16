using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Application.VehicleDocuments.Queries
{
    // Deliberately EXCLUDES FileKey/FileContentType: the uploaded file is only ever
    // reachable through the dedicated download endpoint (GetVehicleDocumentQuery), never as
    // a raw storage key handed to the client. Mirrors DriverRequirementDto. Rows that were
    // never uploaded for a conditional type simply do not appear here — this query never
    // synthesizes a placeholder row.
    public class VehicleDocumentDto
    {
        public required VehicleDocumentType Type { get; set; }
        public required VehicleDocumentStatus Status { get; set; }
        public DateOnly? IssueDate { get; set; }
        public DateOnly? ExpirationDate { get; set; }
        public bool DocumentPresent { get; set; }
        public string? DocumentNumber { get; set; }

        // Resolved display name of whoever approved/rejected the document, never the raw
        // ValidatedBy Guid — a history of "who validated" is only useful if it's legible.
        public string? ValidatedByName { get; set; }
    }
}
