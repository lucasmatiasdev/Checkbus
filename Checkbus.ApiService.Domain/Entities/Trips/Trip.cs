using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Interfaces;

namespace Checkbus.ApiService.Domain.Entities.Trips
{
    // The only entity in this module that carries OrganizationId: Location/Event are
    // shared catalogs, Route/Stop are scoped transitively through this Trip. DriverId,
    // VehicleId and EventId are FK-only, no navigation property back, same pattern as
    // VehicleDocument -> Vehicle. Capacity is a snapshot of Vehicle.Capacity taken at
    // creation time so a later change to the vehicle never affects already-published trips.
    public class Trip : ITenantEntity
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public Organization? Organization { get; set; }
        public required Guid DriverId { get; set; }
        public required Guid VehicleId { get; set; }
        public required Guid EventId { get; set; }
        public DateTime DepartureDate { get; set; }
        public DateTime ArrivalDate { get; set; }
        public int Capacity { get; set; }
        public int AvailableSeats { get; set; }
        public decimal Price { get; set; }
        public TripStatus Status { get; set; } = TripStatus.Programado;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
