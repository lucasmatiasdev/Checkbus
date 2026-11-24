using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Application.SupportTickets.Queries
{

    public class SupportTicketAdminDto
    {
        public required Guid Id { get; set; }
        public required Guid OrganizationId { get; set; }
        public required string OrganizationName { get; set; }
        public SupportTicketCategory Category { get; set; }
        public required string Subject { get; set; }
        public required string Description { get; set; }
        public SupportTicketStatus Status { get; set; }
        public string? AttachmentFileName { get; set; }
        public required DateTime CreatedAt { get; set; }
    }
}
