using MediatR;

namespace Checkbus.ApiService.Application.VehicleDocuments.Queries
{
    public class GetVehicleDocumentsQuery : IRequest<IReadOnlyList<VehicleDocumentDto>>
    {
        public required Guid VehicleId { get; set; }
    }
}
