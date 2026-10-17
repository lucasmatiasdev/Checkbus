using System.Net;
using System.Net.Http.Json;
using Checkbus.Web.Features.PublicReservations.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Web.Features.PublicReservations.Services;

/// <summary>
/// Typed client for <c>api/public/organizations/{orgSlug}/trips</c>
/// (odd/tasks/viajes-disponibles-publico.md T2) — a separate client from
/// <see cref="PublicTripsClient"/> rather than a new method on it: the resource addressed here
/// is an Organization, not a Trip, same single-responsibility split the API side already made
/// with <c>PublicOrganizationsController</c> vs <c>PublicTripsController</c>. Reuses the same
/// keyed <c>"apiservice"</c> <see cref="HttpClient"/> as every other anonymous client in this
/// folder — see <see cref="PublicTripsClient"/>'s own doc comment for why no bearer token is
/// ever attached here.
/// </summary>
public sealed class PublicOrganizationsClient([FromKeyedServices("apiservice")] HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<GetPublicOrganizationTripsOutcome> GetTripsAsync(
        string orgSlug,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? eventName = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildTripsQuery(dateFrom, dateTo, eventName);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(
                $"public/organizations/{Uri.EscapeDataString(orgSlug)}/trips{query}", cancellationToken);
        }
        catch (Exception)
        {
            return new GetPublicOrganizationTripsOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var trips = await response.Content
                            .ReadFromJsonAsync<List<PublicOrganizationTripResponse>>(cancellationToken);
                        return new GetPublicOrganizationTripsOutcome.Success(trips ?? []);

                    case HttpStatusCode.NotFound:
                        return new GetPublicOrganizationTripsOutcome.NotFound();

                    default:
                        return new GetPublicOrganizationTripsOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new GetPublicOrganizationTripsOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    /// <summary>
    /// Builds the optional query string — every filter maps to
    /// <c>GetPublicOrganizationTripsQuery</c>'s matching nullable property on the API side, and
    /// an absent filter is simply omitted rather than sent as an empty value. Dates are sent as
    /// plain <c>yyyy-MM-dd</c>, same convention as <c>TripsReportClient.BuildReportQuery</c>.
    /// </summary>
    internal static string BuildTripsQuery(DateTime? dateFrom, DateTime? dateTo, string? eventName)
    {
        var parts = new List<string>();
        if (dateFrom.HasValue)
            parts.Add($"dateFrom={dateFrom.Value:yyyy-MM-dd}");
        if (dateTo.HasValue)
            parts.Add($"dateTo={dateTo.Value:yyyy-MM-dd}");
        if (!string.IsNullOrWhiteSpace(eventName))
            parts.Add($"eventName={Uri.EscapeDataString(eventName)}");

        return parts.Count == 0 ? string.Empty : $"?{string.Join("&", parts)}";
    }
}
