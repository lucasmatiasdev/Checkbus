using Microsoft.AspNetCore.Http;

namespace Checkbus.ApiService.Contracts
{
    // Multipart/form-data binding model for POST .../document. Bound via [FromForm] on
    // DriverRequirementsController.UploadDocument.
    public sealed class UploadDriverRequirementDocumentRequest
    {
        public required IFormFile File { get; set; }
        public required DateOnly ExpirationDate { get; set; }
        public DateOnly? IssueDate { get; set; }
    }
}
