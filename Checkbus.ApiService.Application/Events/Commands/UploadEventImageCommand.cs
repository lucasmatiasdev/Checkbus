using MediatR;

namespace Checkbus.ApiService.Application.Events.Commands
{
    public class UploadEventImageCommand : IRequest
    {
        public required Guid EventId { get; set; }
        public required Stream FileStream { get; set; }
        public required string ContentType { get; set; }
        public required long FileSizeBytes { get; set; }
        public required string OriginalFileName { get; set; }
    }
}
