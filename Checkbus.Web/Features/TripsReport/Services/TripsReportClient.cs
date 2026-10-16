using System.Net;
using System.Net.Http.Json;
using Checkbus.Web.Features.Trips.Contracts;
using Checkbus.Web.Features.TripsReport.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Web.Features.TripsReport.Services;

/// <summary>
/// Typed client for the <c>api/Trips/report</c> endpoint: the filtered trips report (JSON, for
/// on-screen display). The binary Excel/CSV export is intentionally NOT exposed here — file
/// downloads go through the <c>/trips/report/export</c> minimal-API proxy in
/// <c>Checkbus.Web/Program.cs</c> instead, same pattern as every other file download in this
/// app (see that proxy's comment for why a typed client can't do it).
/// </summary>
/// <remarks>
/// Takes the keyed <c>"apiservice"</c> <see cref="HttpClient"/> via <c>[FromKeyedServices]</c>
/// rather than <see cref="IHttpClientFactory"/> — see
/// <see cref="Checkbus.Web.Extensions.ApplicationScopeHandlerExtensions"/> for why the plain
/// factory-created client would silently miss the bearer token. Because this class is registered
/// as a normal scoped service (see <c>Program.cs</c>), ASP.NET Core's DI container resolves the
/// keyed parameter automatically — upstream consumers just inject <see cref="TripsReportClient"/>
/// normally.
/// </remarks>
public sealed class TripsReportClient([FromKeyedServices("apiservice")] HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<TripsReportOutcome> GetReportAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        WebTripStatus? status = null,
        Guid? driverId = null,
        Guid? vehicleId = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildReportQuery(dateFrom, dateTo, status, driverId, vehicleId);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync($"Trips/report{query}", cancellationToken);
        }
        catch (Exception)
        {
            return new TripsReportOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var report = await response.Content
                            .ReadFromJsonAsync<TripsReportResponse>(cancellationToken);
                        return new TripsReportOutcome.Success(report!);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new TripsReportOutcome.Forbidden();

                    default:
                        return new TripsReportOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new TripsReportOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    /// <summary>
    /// Builds the optional query string — every filter maps to <c>GetTripsReportQuery</c>'s
    /// matching nullable property on the API side, and an absent filter is simply omitted rather
    /// than sent as an empty value. Dates are sent as plain <c>yyyy-MM-dd</c> (the API binds them
    /// as <c>DateOnly?</c>), matching this project's model-binding convention elsewhere.
    /// </summary>
    internal static string BuildReportQuery(
        DateTime? dateFrom,
        DateTime? dateTo,
        WebTripStatus? status,
        Guid? driverId,
        Guid? vehicleId)
    {
        var parts = new List<string>();
        if (dateFrom.HasValue)
            parts.Add($"dateFrom={dateFrom.Value:yyyy-MM-dd}");
        if (dateTo.HasValue)
            parts.Add($"dateTo={dateTo.Value:yyyy-MM-dd}");
        if (status.HasValue)
            parts.Add($"status={(int)status.Value}");
        if (driverId.HasValue)
            parts.Add($"driverId={Uri.EscapeDataString(driverId.Value.ToString())}");
        if (vehicleId.HasValue)
            parts.Add($"vehicleId={Uri.EscapeDataString(vehicleId.Value.ToString())}");

        return parts.Count == 0 ? string.Empty : $"?{string.Join("&", parts)}";
    }
}
