using Checkbus.Web.Features.SupportTickets.Contracts;

namespace Checkbus.Web.Features.SupportTickets.Services;

/// <summary>
/// Closed outcome of a <c>POST /api/SupportTickets</c> call, covering every realistic server
/// response so callers can pattern-match on the result without inspecting HTTP status codes
/// directly. Mirrors <see cref="Checkbus.Web.Features.VehicleDocuments.Services.VehicleDocumentActionOutcome"/>'s
/// shape, except <see cref="Success"/> carries the created ticket — unlike the vehicle document
/// actions, this endpoint returns <c>201 Created</c> with a body, not <c>204 No Content</c> — and
/// there is no 404 case: creating a ticket has no id to not-find. The private constructor confines
/// the hierarchy to the nested cases declared here; a consumer can exhaustively switch over
/// <see cref="Success"/>, <see cref="ValidationFailed"/>, <see cref="Forbidden"/>, and
/// <see cref="TransportError"/>.
/// </summary>
public abstract record SupportTicketCreateOutcome
{
    private SupportTicketCreateOutcome()
    {
    }

    /// <summary>201 Created — the ticket was created successfully.</summary>
    public sealed record Success(SupportTicketResponse Ticket) : SupportTicketCreateOutcome;

    /// <summary>400 Bad Request — FluentValidation failures keyed by property name.</summary>
    public sealed record ValidationFailed(IReadOnlyDictionary<string, string[]> Errors) : SupportTicketCreateOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : SupportTicketCreateOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : SupportTicketCreateOutcome;
}
