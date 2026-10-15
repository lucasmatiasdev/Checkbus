namespace Checkbus.Web.Features.SupportTickets.Services;

/// <summary>
/// Closed outcome of a <c>PATCH /api/support-tickets-admin/{id}/status</c> call, covering every
/// realistic server response so callers can pattern-match on the result without inspecting HTTP
/// status codes directly. Mirrors <see cref="SupportTicketsListOutcome"/>'s shape: the endpoint
/// returns <c>204 No Content</c> on success (nothing to carry) and has no validation-failure case
/// exposed here — <see cref="Checkbus.Web.Features.SupportTickets.Contracts.WebSupportTicketStatus"/>
/// is a closed enum selected from a <c>MudSelect</c>, so an invalid status value is not a realistic
/// client input; an unexpected 400 still falls through to <see cref="TransportError"/>. The private
/// constructor confines the hierarchy to the nested cases declared here; a consumer can
/// exhaustively switch over <see cref="Success"/>, <see cref="Forbidden"/>, and
/// <see cref="TransportError"/>.
/// </summary>
public abstract record UpdateSupportTicketStatusOutcome
{
    private UpdateSupportTicketStatusOutcome()
    {
    }

    /// <summary>204 No Content — the ticket's status was updated successfully.</summary>
    public sealed record Success : UpdateSupportTicketStatusOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : UpdateSupportTicketStatusOutcome;

    /// <summary>
    /// Any other transport-level failure (connection error, client-side timeout, unexpected status
    /// code such as 400/404, etc). Message is short and user-presentable, never a raw exception
    /// detail.
    /// </summary>
    public sealed record TransportError(string Message) : UpdateSupportTicketStatusOutcome;
}
