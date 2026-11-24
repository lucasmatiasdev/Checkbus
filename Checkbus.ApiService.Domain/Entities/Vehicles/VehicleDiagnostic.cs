namespace Checkbus.ApiService.Domain.Entities.Vehicles
{

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
