using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.VehicleDocuments.Commands
{
    public class UploadVehicleDocumentCommand : IRequest
    {
        public required Guid VehicleId { get; set; }
        public required VehicleDocumentType Type { get; set; }
        public required Stream FileStream { get; set; }
        public required string ContentType { get; set; }
        public required long FileSizeBytes { get; set; }
        public required string OriginalFileName { get; set; }
        public required DateOnly ExpirationDate { get; set; }
        public DateOnly? IssueDate { get; set; }
    }
}
