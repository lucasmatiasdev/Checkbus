using Microsoft.AspNetCore.Http;

namespace Checkbus.ApiService.Contracts
{

    public sealed class UploadDriverRequirementDocumentRequest
    {
        public required IFormFile File { get; set; }
        public required DateOnly ExpirationDate { get; set; }
        public DateOnly? IssueDate { get; set; }
    }
}
