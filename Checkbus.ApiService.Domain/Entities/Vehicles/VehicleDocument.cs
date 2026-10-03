using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Domain.Entities.Vehicles
{
    // One row per (Vehicle, Type) pair, enforced by a unique index on (VehicleId, Type) in
    // CheckbusDbContext. Deliberately no navigation property back to Vehicle — the
    // relationship stays FK-only, same pattern as DriverRequirement -> User. Vigencia
    // (expired/expiring) is never stored here: only ExpirationDate is persisted, and the
    // "about to expire or expired" determination happens at query/render time.
    public class VehicleDocument
    {
        public Guid Id { get; set; }
        public required Guid VehicleId { get; set; }
        public required VehicleDocumentType Type { get; set; }
        public VehicleDocumentStatus Status { get; set; } = VehicleDocumentStatus.Pendiente;
        public DateOnly? IssueDate { get; set; }
        public DateOnly? ExpirationDate { get; set; }
        public bool DocumentPresent { get; set; }
        public string? FileKey { get; set; }

        // Captured from the uploaded file's Content-Type at upload time so the download
        // endpoint can serve the correct header without guessing from the file extension.
        public string? FileContentType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
