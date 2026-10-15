using Checkbus.ApiService.Application.SupportTickets.Queries;
using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.SupportTickets.Commands
{
    // OrganizationId/CreatedByUserId are deliberately absent here, same reasoning as
    // CreateEventCommand's LocationName/Address fields vs. CreateEventCommandHandler resolving
    // the tenant from ICurrentUserService: the caller never gets to pick who the ticket belongs
    // to or who created it.
    public class CreateSupportTicketCommand : IRequest<SupportTicketDto>
    {
        public required SupportTicketCategory Category { get; set; }
        public required string Subject { get; set; }
        public required string Description { get; set; }

        // Optional single attachment - same FileStream/ContentType/OriginalFileName shape as
        // UploadVehicleDocumentCommand, but nullable here because a ticket may have no
        // attachment at all.
        public Stream? AttachmentStream { get; set; }
        public string? AttachmentContentType { get; set; }
        public string? AttachmentFileName { get; set; }
    }
}
