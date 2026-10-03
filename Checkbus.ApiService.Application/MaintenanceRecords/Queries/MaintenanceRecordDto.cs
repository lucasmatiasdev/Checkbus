using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Application.MaintenanceRecords.Queries
{
    public class MaintenanceRecordDto
    {
        public required Guid Id { get; set; }
        public required Guid VehicleId { get; set; }
        public required MaintenanceType Type { get; set; }
        public DateOnly ScheduledDate { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public MaintenanceStatus Status { get; set; }
        public required string Description { get; set; }
        public string? MechanicNotes { get; set; }
        public decimal? Cost { get; set; }

        // Display-only join fields from the owning Vehicle — same shape as
        // VehicleListItemDto's Brand/Model/Patent — avoids a second round trip from the
        // standalone (non-vehicle-nested) maintenance UI to render the list/detail.
        public string? VehiclePatent { get; set; }
        public string? VehicleBrand { get; set; }
        public string? VehicleModel { get; set; }
    }
}
