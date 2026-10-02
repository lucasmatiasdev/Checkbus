using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.DriverRequirements.Commands
{
    public class UploadDriverRequirementDocumentCommand : IRequest
    {
        public required Guid TargetUserId { get; set; }
        public required DriverRequirementType Type { get; set; }
        public required Stream FileStream { get; set; }
        public required string ContentType { get; set; }
        public required long FileSizeBytes { get; set; }
        public required string OriginalFileName { get; set; }
        public required DateOnly ExpirationDate { get; set; }
        public DateOnly? IssueDate { get; set; }
    }
}
