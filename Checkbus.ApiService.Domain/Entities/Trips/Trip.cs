using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Interfaces;

namespace Checkbus.ApiService.Domain.Entities.Trips
{

    public class Trip : ITenantEntity
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public Organization? Organization { get; set; }
        public required Guid DriverId { get; set; }
        public required Guid VehicleId { get; set; }
        public required Guid EventId { get; set; }

        public required string Slug { get; set; }
        public DateTime DepartureDate { get; set; }
        public DateTime ArrivalDate { get; set; }
        public int Capacity { get; set; }
        public int AvailableSeats { get; set; }
        public decimal Price { get; set; }
        public TripStatus Status { get; set; } = TripStatus.Programado;

        public DateTime? ActualArrivalDate { get; set; }
        public string? Observations { get; set; }

        public bool LiveTrackingEnabled { get; set; }
        public double? CurrentLatitude { get; set; }
        public double? CurrentLongitude { get; set; }
        public DateTime? LocationUpdatedAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
