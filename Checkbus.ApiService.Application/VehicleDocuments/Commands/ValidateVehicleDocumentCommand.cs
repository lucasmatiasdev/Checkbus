using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.VehicleDocuments.Commands
{
    public class ValidateVehicleDocumentCommand : IRequest
    {
        public required Guid VehicleId { get; set; }
        public required VehicleDocumentType Type { get; set; }
        public required bool Approved { get; set; }
    }
}
