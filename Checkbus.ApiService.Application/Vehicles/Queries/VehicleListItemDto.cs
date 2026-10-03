using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Application.Vehicles.Queries
{
    public class VehicleListItemDto
    {
        public Guid Id { get; set; }
        public required string Brand { get; set; }
        public required string Model { get; set; }
        public int Year { get; set; }
        public required string Patent { get; set; }
        public int Capacity { get; set; }
        public int Mileage { get; set; }
        public VehicleStatus Status { get; set; }
        public VehicleOwnerType OwnerType { get; set; }
        public Guid? OwnerUserId { get; set; }
        public string? OwnerName { get; set; }
        public string? OwnerDocumentNumber { get; set; }
    }
}
