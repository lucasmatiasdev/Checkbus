using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Interfaces;

namespace Checkbus.ApiService.Domain.Entities.Vehicles
{

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

        public Guid? OwnerUserId { get; set; }

        public string? OwnerName { get; set; }
        public string? OwnerDocumentNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
