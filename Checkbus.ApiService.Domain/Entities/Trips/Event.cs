using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Interfaces;

namespace Checkbus.ApiService.Domain.Entities.Trips
{
    // Private per organization — events are not shared across tenants. FK-only relationship
    // to Location, no navigation property back.
    public class Event : ITenantEntity
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public EventType Type { get; set; }
        public DateTime Date { get; set; }
        public required Guid LocationId { get; set; }
        public Guid OrganizationId { get; set; }
        public Organization? Organization { get; set; }

        // Opaque storage key for an optional promotional image (ad spot / band photo), same
        // FileKey/FileContentType shape as VehicleDocument — never a raw URL, served through a
        // download endpoint that resolves the key via IFileStorageService.
        public string? ImageKey { get; set; }
        public string? ImageContentType { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
