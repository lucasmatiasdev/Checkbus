using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Domain.Entities.Support
{

    public class SupportTicket
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid CreatedByUserId { get; set; }
        public required SupportTicketCategory Category { get; set; }
        public required string Subject { get; set; }
        public required string Description { get; set; }
        public SupportTicketStatus Status { get; set; }

        public string? AttachmentKey { get; set; }
        public string? AttachmentFileName { get; set; }
        public string? AttachmentContentType { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
