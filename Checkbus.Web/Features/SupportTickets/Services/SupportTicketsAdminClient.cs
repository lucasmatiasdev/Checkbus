using System.Net;
using System.Net.Http.Json;
using Checkbus.Web.Features.SupportTickets.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Web.Features.SupportTickets.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/support-tickets-admin</c> call, covering every realistic
/// server response so callers can pattern-match on the result without inspecting HTTP status
/// codes directly. Mirrors <see cref="SupportTicketsListOutcome"/>'s shape — the client-side
/// equivalent for the Checkbus internal staff panel's cross-tenant list rather than a single
/// organization's own tickets. The private constructor confines the hierarchy to the nested cases
/// declared here; a consumer can exhaustively switch over <see cref="Success"/>,
/// <see cref="Forbidden"/>, and <see cref="TransportError"/>.
/// </summary>
public abstract record SupportTicketsAdminListOutcome
{
    private SupportTicketsAdminListOutcome()
    {
    }

    /// <summary>200 OK — every organization's tickets were fetched successfully.</summary>
    public sealed record Success(IReadOnlyList<SupportTicketAdminResponse> Tickets) : SupportTicketsAdminListOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : SupportTicketsAdminListOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : SupportTicketsAdminListOutcome;
}

/// <summary>
/// Typed client for the <c>api/support-tickets-admin</c> endpoints: the Checkbus internal staff
/// panel's cross-tenant ticket list and status update, gated server-side to the
/// <c>CheckbusSoporte</c> role only (see <c>Checkbus.ApiService.Controllers.SupportTicketsAdminController</c>).
/// Maps every realistic server response into the matching closed outcome type so callers never
/// need to inspect HTTP status codes directly. Deliberately separate from
/// <see cref="SupportTicketsClient"/>, same way its controller is separate from
/// <c>SupportTicketsController</c> — this client's audience is Checkbus staff, not a client
/// organization's own users.
/// </summary>
/// <remarks>
/// Takes the keyed <c>"apiservice"</c> <see cref="HttpClient"/> via <c>[FromKeyedServices]</c>
/// rather than <see cref="IHttpClientFactory"/> — see
/// <see cref="Checkbus.Web.Extensions.ApplicationScopeHandlerExtensions"/> for why the plain
/// factory-created client would silently miss the bearer token. Because this class is registered
/// as a normal scoped service (see <c>Program.cs</c>), ASP.NET Core's DI container resolves the
/// keyed parameter automatically — upstream consumers just inject
/// <see cref="SupportTicketsAdminClient"/> normally.
/// </remarks>
public sealed class SupportTicketsAdminClient([FromKeyedServices("apiservice")] HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<SupportTicketsAdminListOutcome> GetAllTicketsAsync(
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            // The keyed "apiservice" HttpClient's BaseAddress already ends in "/api/" (see
            // Program.cs), so this relative path resolves to "api/support-tickets-admin" — the
            // controller's literal route, not the "api/[controller]" token SupportTicketsClient's
            // target uses.
            response = await _httpClient.GetAsync("support-tickets-admin", cancellationToken);
        }
        catch (Exception)
        {
            return new SupportTicketsAdminListOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var tickets = await response.Content
                            .ReadFromJsonAsync<IReadOnlyList<SupportTicketAdminResponse>>(cancellationToken);
                        return new SupportTicketsAdminListOutcome.Success(tickets ?? []);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new SupportTicketsAdminListOutcome.Forbidden();

                    default:
                        return new SupportTicketsAdminListOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new SupportTicketsAdminListOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    public async Task<UpdateSupportTicketStatusOutcome> UpdateStatusAsync(
        Guid ticketId,
        WebSupportTicketStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PatchAsync(
                $"support-tickets-admin/{ticketId}/status",
                JsonContent.Create(new { newStatus }),
                cancellationToken);
        }
        catch (Exception)
        {
            return new UpdateSupportTicketStatusOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.NoContent:
                    return new UpdateSupportTicketStatusOutcome.Success();

                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Forbidden:
                    return new UpdateSupportTicketStatusOutcome.Forbidden();

                default:
                    return new UpdateSupportTicketStatusOutcome.TransportError(
                        "An unexpected error occurred. Please try again.");
            }
        }
    }
}
