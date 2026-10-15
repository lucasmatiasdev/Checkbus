using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.ApiService.Domain.Entities.Support
{
    // Private per organization — a ticket is opened by an organization towards Checkbus (the
    // provider), never shared across tenants. FK-only relationships to Organization and User,
    // same pattern as VehicleDocument -> Vehicle / DriverRequirement -> User: no navigation
    // property back. Status starts at SupportTicketStatus.Abierto, set by the create handler
    // (not here) and is read-only from this session's endpoints — changing it is out of scope
    // (future internal Checkbus staff panel).
    public class SupportTicket
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid CreatedByUserId { get; set; }
        public required SupportTicketCategory Category { get; set; }
        public required string Subject { get; set; }
        public required string Description { get; set; }
        public required SupportTicketPriority Priority { get; set; }
        public SupportTicketStatus Status { get; set; }

        // Opaque storage key for an optional single attachment, same FileKey/FileContentType
        // shape as VehicleDocument — never a raw URL, served through a download endpoint that
        // resolves the key via IFileStorageService.
        public string? AttachmentKey { get; set; }
        public string? AttachmentFileName { get; set; }
        public string? AttachmentContentType { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
