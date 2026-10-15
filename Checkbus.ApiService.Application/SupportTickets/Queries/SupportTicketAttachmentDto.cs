namespace Checkbus.ApiService.Application.SupportTickets.Queries
{
    // Carries the opaque storage key back to the controller only - same split as
    // GetEventImageQuery/GetPublicTripEventImageQuery, where the handler resolves tenant
    // ownership and the controller is the only layer that talks to IFileStorageService.
    public class SupportTicketAttachmentDto
    {
        public required string AttachmentKey { get; set; }
        public string? AttachmentFileName { get; set; }
        public string? AttachmentContentType { get; set; }
    }
}
