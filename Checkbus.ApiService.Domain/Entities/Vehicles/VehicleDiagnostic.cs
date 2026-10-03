namespace Checkbus.ApiService.Domain.Entities.Vehicles
{
    // One row per diagnostic event, always tied to a MaintenanceRecord. VehicleId is a
    // denormalized FK kept alongside MaintenanceRecordId (explicit decision — a diagnostic
    // never exists without its parent maintenance record). Both relationships stay FK-only,
    // no navigation properties back, same pattern as VehicleDocument -> Vehicle.
    public class VehicleDiagnostic
    {
        public Guid Id { get; set; }
        public required Guid VehicleId { get; set; }
        public required Guid MaintenanceRecordId { get; set; }
        public DateOnly DiagnosedAt { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
