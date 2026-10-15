using Checkbus.Web.Features.SupportTickets.Contracts;

namespace Checkbus.Web.Features.SupportTickets.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/SupportTickets</c> call, covering every realistic server
/// response so callers can pattern-match on the result without inspecting HTTP status codes
/// directly. Mirrors <see cref="Checkbus.Web.Features.VehicleDocuments.Services.VehicleDocumentsListOutcome"/>'s
/// shape, minus the 404 case — the endpoint lists the caller's own organization's tickets and has
/// no id to not-find. The private constructor confines the hierarchy to the nested cases declared
/// here; a consumer can exhaustively switch over <see cref="Success"/>, <see cref="Forbidden"/>,
/// and <see cref="TransportError"/>.
/// </summary>
public abstract record SupportTicketsListOutcome
{
    private SupportTicketsListOutcome()
    {
    }

    /// <summary>200 OK — the organization's tickets were fetched successfully.</summary>
    public sealed record Success(IReadOnlyList<SupportTicketResponse> Tickets) : SupportTicketsListOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : SupportTicketsListOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : SupportTicketsListOutcome;
}
