using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.Vehicles.Commands
{
    public class CreateVehicleCommand : IRequest<CreateVehicleCommandResult>
    {
        public required string Brand { get; set; }
        public required string Model { get; set; }
        public required int Year { get; set; }
        public required string Patent { get; set; }
        public required int Capacity { get; set; }
        public required int Mileage { get; set; }
        public required VehicleStatus Status { get; set; }
        public required VehicleOwnerType OwnerType { get; set; }

        // Conditional on OwnerType — see CreateVehicleCommandValidator.
        public Guid? OwnerUserId { get; set; }
        public string? OwnerName { get; set; }
        public string? OwnerDocumentNumber { get; set; }

        // Deliberately ABSENT: OrganizationId (from the validated token — a body-supplied
        // value would be a cross-tenant IDOR), same pattern as RegisterUserCommand.
    }

    public class CreateVehicleCommandResult
    {
        public required Guid VehicleId { get; set; }
        public required string Brand { get; set; }
        public required string Model { get; set; }
        public required int Year { get; set; }
        public required string Patent { get; set; }
        public required int Capacity { get; set; }
        public required int Mileage { get; set; }
        public required VehicleStatus Status { get; set; }
        public required VehicleOwnerType OwnerType { get; set; }
        public Guid? OwnerUserId { get; set; }
        public string? OwnerName { get; set; }
        public string? OwnerDocumentNumber { get; set; }
        public required Guid OrganizationId { get; set; }
    }
}
