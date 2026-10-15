namespace Checkbus.Web.Features.SupportTickets.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.SupportTickets.Queries.SupportTicketAdminDto</c>, returned
/// by <c>GET /api/support-tickets-admin</c> (200 array items). Checkbus.Web does not reference the
/// API projects (pure BFF), so this DTO is duplicated here rather than shared. Same shape as
/// <see cref="SupportTicketResponse"/> (the client-organization "my tickets" DTO), plus
/// <see cref="OrganizationId"/>/<see cref="OrganizationName"/> — this is the cross-tenant Checkbus
/// internal staff panel's DTO, so every ticket needs to carry which organization it belongs to.
/// Like <see cref="SupportTicketResponse"/>, this deliberately excludes the file storage key: the
/// attachment is never reachable through the raw storage key, only through
/// <see cref="AttachmentFileName"/> as a label.
/// </summary>
public sealed record SupportTicketAdminResponse
{
    public required Guid Id { get; init; }
    public required Guid OrganizationId { get; init; }
    public required string OrganizationName { get; init; }
    public required WebSupportTicketCategory Category { get; init; }
    public required string Subject { get; init; }
    public required string Description { get; init; }
    public required WebSupportTicketPriority Priority { get; init; }
    public required WebSupportTicketStatus Status { get; init; }
    public string? AttachmentFileName { get; init; }
    public required DateTime CreatedAt { get; init; }
}
