using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Application.SupportTickets.Queries
{
    public class SupportTicketDto
    {
        public required Guid Id { get; set; }
        public SupportTicketCategory Category { get; set; }
        public required string Subject { get; set; }
        public required string Description { get; set; }
        public SupportTicketStatus Status { get; set; }

        // Mirrors EventDto's HasImage/VehicleDocumentResponse's never-expose-the-raw-key
        // reasoning, but here the actual file name IS surfaced (unlike the opaque storage
        // key) because the caller needs it to label/download their own attachment.
        public string? AttachmentFileName { get; set; }

        public required DateTime CreatedAt { get; set; }
    }
}
