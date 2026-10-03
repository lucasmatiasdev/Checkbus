using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Domain.Entities.Vehicles
{
    // One row per service visit. Deliberately no navigation property back to Vehicle — the
    // relationship stays FK-only, same pattern as VehicleDocument -> Vehicle. No
    // next-service scheduling fields (NextServiceDate/NextServiceMileage) by explicit
    // decision: ScheduledDate/StartDate/EndDate/Status only cover this record's own history.
    public class MaintenanceRecord
    {
        public Guid Id { get; set; }
        public required Guid VehicleId { get; set; }
        public required MaintenanceType Type { get; set; }
        public DateOnly ScheduledDate { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Programado;
        public required string Description { get; set; }
        public string? MechanicNotes { get; set; }
        public decimal? Cost { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
