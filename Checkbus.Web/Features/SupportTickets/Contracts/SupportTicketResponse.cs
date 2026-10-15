namespace Checkbus.Web.Features.SupportTickets.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.SupportTickets.Queries.SupportTicketDto</c>, returned both by
/// <c>POST /api/SupportTickets</c> (201 body) and <c>GET /api/SupportTickets</c> (200 array items).
/// Checkbus.Web does not reference the API projects (pure BFF), so this DTO is duplicated here
/// rather than shared. Like <see cref="Checkbus.Web.Features.VehicleDocuments.Contracts.VehicleDocumentResponse"/>,
/// this deliberately excludes the file storage key: the attachment is never reachable through the
/// raw storage key, only through <see cref="AttachmentFileName"/> as a label.
/// </summary>
public sealed record SupportTicketResponse
{
    public required Guid Id { get; init; }
    public required WebSupportTicketCategory Category { get; init; }
    public required string Subject { get; init; }
    public required string Description { get; init; }
    public required WebSupportTicketStatus Status { get; init; }
    public string? AttachmentFileName { get; init; }
    public required DateTime CreatedAt { get; init; }
}
