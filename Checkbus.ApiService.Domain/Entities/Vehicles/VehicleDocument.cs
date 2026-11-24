using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Domain.Entities.Vehicles
{

    public class VehicleDocument
    {
        public Guid Id { get; set; }
        public required Guid VehicleId { get; set; }
        public required VehicleDocumentType Type { get; set; }
        public VehicleDocumentStatus Status { get; set; } = VehicleDocumentStatus.Pendiente;
        public DateOnly? IssueDate { get; set; }
        public DateOnly? ExpirationDate { get; set; }
        public bool DocumentPresent { get; set; }

        public string? DocumentNumber { get; set; }

        public Guid? ValidatedBy { get; set; }
        public string? FileKey { get; set; }

        public string? FileContentType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
