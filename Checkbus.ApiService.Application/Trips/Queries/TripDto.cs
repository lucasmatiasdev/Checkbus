using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    public class TripDto
    {
        public required Guid Id { get; set; }
        public required Guid VehicleId { get; set; }
        public required Guid DriverId { get; set; }
        public required Guid EventId { get; set; }
        public DateTime DepartureDate { get; set; }
        public DateTime ArrivalDate { get; set; }
        public int Capacity { get; set; }
        public int AvailableSeats { get; set; }
        public decimal Price { get; set; }
        public TripStatus Status { get; set; }

        // Display-only join fields — same shape choice as MaintenanceRecordDto's
        // VehiclePatent/Brand/Model — avoids a second round trip to render the Trips list/detail.
        public string? VehiclePatent { get; set; }
        public string? VehicleBrand { get; set; }
        public string? VehicleModel { get; set; }
        public string? DriverName { get; set; }
        public string? DriverSurname { get; set; }
        public string? EventName { get; set; }
    }
}
