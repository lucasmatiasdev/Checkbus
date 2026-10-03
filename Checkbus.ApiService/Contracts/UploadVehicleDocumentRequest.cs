using Microsoft.AspNetCore.Http;

namespace Checkbus.ApiService.Contracts
{
    // Multipart/form-data binding model for POST .../document. Bound via [FromForm] on
    // VehicleDocumentsController.UploadDocument. Mirrors
    // UploadDriverRequirementDocumentRequest's shape.
    public sealed class UploadVehicleDocumentRequest
    {
        public required IFormFile File { get; set; }
        public required DateOnly ExpirationDate { get; set; }
        public DateOnly? IssueDate { get; set; }
    }
}
