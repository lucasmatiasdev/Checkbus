using System.Net;
using System.Net.Http.Json;
using Checkbus.Web.Features.Audit.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Web.Features.Audit.Services;

/// <summary>
/// Typed client for the <c>api/audit-log</c> endpoint (CU-16, <c>odd/tasks/auditoria-sistema.md</c>),
/// gated server-side to the <c>Administrador</c> and <c>CheckbusSoporte</c> roles only (see
/// <c>Checkbus.ApiService.Controllers.AuditLogController</c>). Maps every realistic server
/// response into the matching closed <see cref="AuditLogListOutcome"/> so callers never need to
/// inspect HTTP status codes directly.
/// </summary>
/// <remarks>
/// Takes the keyed <c>"apiservice"</c> <see cref="HttpClient"/> via <c>[FromKeyedServices]</c>
/// rather than <see cref="IHttpClientFactory"/> — see
/// <see cref="Checkbus.Web.Extensions.ApplicationScopeHandlerExtensions"/> for why the plain
/// factory-created client would silently miss the bearer token. Because this class is registered
/// as a normal scoped service (see <c>Program.cs</c>), ASP.NET Core's DI container resolves the
/// keyed parameter automatically — upstream consumers just inject <see cref="AuditLogClient"/>
/// normally.
/// </remarks>
public sealed class AuditLogClient([FromKeyedServices("apiservice")] HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<AuditLogListOutcome> GetAuditLogAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        WebAuditAction? action = null,
        Guid? actorUserId = null,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(dateFrom, dateTo, action, actorUserId, organizationId);

        HttpResponseMessage response;
        try
        {
            // The keyed "apiservice" HttpClient's BaseAddress already ends in "/api/" (see
            // Program.cs), so this relative path resolves to "api/audit-log" — the controller's
            // literal route.
            response = await _httpClient.GetAsync($"audit-log{query}", cancellationToken);
        }
        catch (Exception)
        {
            return new AuditLogListOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var entries = await response.Content
                            .ReadFromJsonAsync<IReadOnlyList<AuditLogResponse>>(cancellationToken);
                        return new AuditLogListOutcome.Success(entries ?? []);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new AuditLogListOutcome.Forbidden();

                    default:
                        return new AuditLogListOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new AuditLogListOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    /// <summary>
    /// Builds the optional query string — every filter maps to <c>GetAuditLogQuery</c>'s matching
    /// nullable property on the API side, and an absent filter is simply omitted rather than sent
    /// as an empty value. <paramref name="action"/> is sent as its numeric value rather than its
    /// name: ASP.NET Core's default enum model binding (<c>Enum.TryParse</c>) accepts either, and
    /// a numeric value stays correct regardless of whether a <c>JsonStringEnumConverter</c> is
    /// ever added later — same convention as <c>TripsReportClient.BuildReportQuery</c>'s
    /// <c>status</c> filter. <paramref name="dateTo"/> is expected to already be an
    /// end-of-day-inclusive value (see the "Auditoría" page's <c>LoadAuditLogAsync</c>) because
    /// the API compares it directly against <c>AuditLogEntry.CreatedAt</c>, a precise timestamp,
    /// not a date-only column.
    /// </summary>
    internal static string BuildQuery(
        DateTime? dateFrom,
        DateTime? dateTo,
        WebAuditAction? action,
        Guid? actorUserId,
        Guid? organizationId)
    {
        var parts = new List<string>();
        if (dateFrom.HasValue)
            parts.Add($"dateFrom={Uri.EscapeDataString(dateFrom.Value.ToString("o"))}");
        if (dateTo.HasValue)
            parts.Add($"dateTo={Uri.EscapeDataString(dateTo.Value.ToString("o"))}");
        if (action.HasValue)
            parts.Add($"action={(int)action.Value}");
        if (actorUserId.HasValue)
            parts.Add($"actorUserId={Uri.EscapeDataString(actorUserId.Value.ToString())}");
        if (organizationId.HasValue)
            parts.Add($"organizationId={Uri.EscapeDataString(organizationId.Value.ToString())}");

        return parts.Count == 0 ? string.Empty : $"?{string.Join("&", parts)}";
    }
}
