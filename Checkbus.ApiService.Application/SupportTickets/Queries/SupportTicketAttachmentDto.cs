namespace Checkbus.ApiService.Application.SupportTickets.Queries
{

    public class SupportTicketAttachmentDto
    {
        public required string AttachmentKey { get; set; }
        public string? AttachmentFileName { get; set; }
        public string? AttachmentContentType { get; set; }
    }
}
