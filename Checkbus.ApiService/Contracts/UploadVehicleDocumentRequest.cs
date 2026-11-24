using Microsoft.AspNetCore.Http;

namespace Checkbus.ApiService.Contracts
{

    public sealed class UploadVehicleDocumentRequest
    {
        public required IFormFile File { get; set; }
        public required string DocumentNumber { get; set; }
        public required DateOnly ExpirationDate { get; set; }
        public DateOnly? IssueDate { get; set; }
    }
}
