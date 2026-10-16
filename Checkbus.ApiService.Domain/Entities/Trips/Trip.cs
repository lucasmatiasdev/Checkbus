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

        // Human-readable public identifier (e.g. "river-vs-boca-20261025"), unique per
        // organization. Generated once at creation time (CreateTripCommandHandler) — the
        // existing api/public/trips/{id:guid}/... routes keep using the Guid; this is only
        // resolved by a separate slug->Guid endpoint for the public-facing share link.
        public required string Slug { get; set; }
        public DateTime DepartureDate { get; set; }
        public DateTime ArrivalDate { get; set; }
        public int Capacity { get; set; }
        public int AvailableSeats { get; set; }
        public decimal Price { get; set; }
        public TripStatus Status { get; set; } = TripStatus.Programado;

        // CU-08: set by FinishTripCommandHandler when the trip transitions to Completado.
        // ArrivalDate above stays the originally scheduled value (never overwritten) so a
        // delay/early-arrival is always comparable against what was planned.
        public DateTime? ActualArrivalDate { get; set; }
        public string? Observations { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
