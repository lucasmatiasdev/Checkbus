using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.VehicleDocuments.Queries
{
    public class GetVehicleDocumentQuery : IRequest<VehicleDocumentFileDto>
    {
        public required Guid VehicleId { get; set; }
        public required VehicleDocumentType Type { get; set; }
    }
}
