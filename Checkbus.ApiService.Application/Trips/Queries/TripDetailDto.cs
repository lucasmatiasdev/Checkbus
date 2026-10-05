using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    // Same display fields as TripDto, plus the nested Route/Stops detail — kept as a separate
    // type (rather than reusing TripDto) because GetTripsQueryHandler (list) deliberately never
    // resolves Route/Stops/Location per item to avoid an N+1 fan-out across the whole list.
    public class TripDetailDto
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

        public string? VehiclePatent { get; set; }
        public string? VehicleBrand { get; set; }
        public string? VehicleModel { get; set; }
        public string? DriverName { get; set; }
        public string? DriverSurname { get; set; }
        public string? EventName { get; set; }

        public required RouteDto Route { get; set; }

        public class RouteDto
        {
            public TimeSpan EstimatedDuration { get; set; }
            public decimal DistanceKm { get; set; }
            public required IReadOnlyList<StopDto> Stops { get; set; }
        }

        public class StopDto
        {
            public int Order { get; set; }
            public StopType Type { get; set; }
            public required string Name { get; set; }
            public required string Address { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }
    }
}
