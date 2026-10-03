using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Interfaces;

namespace Checkbus.ApiService.Domain.Entities.Vehicles
{
    // OrganizationId/Organization is the operational "used by this org" link only (the
    // ITenantEntity scoping used for filtering/authorization) — it is never legal
    // titularidad. Ownership is a separate classification carried by OwnerType plus its
    // conditional fields (OwnerUserId for Chofer, OwnerName/OwnerDocumentNumber for Otro),
    // kept independently settable per notes/Politica_documentacion_vehiculos.docx.
    public class Vehicle : ITenantEntity
    {
        public Guid Id { get; set; }
        public required string Brand { get; set; }
        public required string Model { get; set; }
        public int Year { get; set; }
        public required string Patent { get; set; }
        public int Capacity { get; set; }
        public int Mileage { get; set; }
        public VehicleStatus Status { get; set; }
        public Guid OrganizationId { get; set; }
        public Organization? Organization { get; set; }
        public VehicleOwnerType OwnerType { get; set; }

        // FK-only, no navigation property: only set when OwnerType == Chofer, and must
        // point at a User with Role == Chofer in the same organization.
        public Guid? OwnerUserId { get; set; }

        // Only set when OwnerType == Otro.
        public string? OwnerName { get; set; }
        public string? OwnerDocumentNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
