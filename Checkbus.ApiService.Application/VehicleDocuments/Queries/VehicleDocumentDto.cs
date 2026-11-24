using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Application.VehicleDocuments.Queries
{

    public class VehicleDocumentDto
    {
        public required VehicleDocumentType Type { get; set; }
        public required VehicleDocumentStatus Status { get; set; }
        public DateOnly? IssueDate { get; set; }
        public DateOnly? ExpirationDate { get; set; }
        public bool DocumentPresent { get; set; }
        public string? DocumentNumber { get; set; }

        public string? ValidatedByName { get; set; }
    }
}
