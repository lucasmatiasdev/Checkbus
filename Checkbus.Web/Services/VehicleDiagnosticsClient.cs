using System.Net;
using System.Net.Http.Json;
using Checkbus.Web.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Web.Services;

/// <summary>
/// Typed client for the <c>api/VehicleDiagnostics</c> endpoints: creating a diagnostic (with its
/// atomically-created <c>ComponentDiagnostic</c> rows) and listing diagnostics for a given
/// maintenance record. Maps every realistic server response into the matching closed outcome type
/// so callers never need to inspect HTTP status codes directly.
/// </summary>
/// <remarks>
/// Takes the keyed <c>"apiservice"</c> <see cref="HttpClient"/> via <c>[FromKeyedServices]</c>
/// rather than <see cref="IHttpClientFactory"/> — see
/// <see cref="Checkbus.Web.Extensions.ApplicationScopeHandlerExtensions"/> for why the plain
/// factory-created client would silently miss the bearer token. Because this class is registered
/// as a normal scoped service (see <c>Program.cs</c>), ASP.NET Core's DI container resolves the
/// keyed parameter automatically — upstream consumers just inject
/// <see cref="VehicleDiagnosticsClient"/> normally.
/// </remarks>
public sealed class VehicleDiagnosticsClient([FromKeyedServices("apiservice")] HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<VehicleDiagnosticActionOutcome> CreateAsync(
        CreateVehicleDiagnosticRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("VehicleDiagnostics", request, cancellationToken);
        }
        catch (Exception)
        {
            return new VehicleDiagnosticActionOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Created:
                        var diagnostic = await response.Content
                            .ReadFromJsonAsync<VehicleDiagnosticResponse>(cancellationToken);
                        return new VehicleDiagnosticActionOutcome.Success(diagnostic!);

                    case HttpStatusCode.BadRequest:
                        var validationProblem = await response.Content
                            .ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
                        IReadOnlyDictionary<string, string[]> errors = validationProblem is null
                            ? new Dictionary<string, string[]>()
                            : (IReadOnlyDictionary<string, string[]>)validationProblem.Errors;
                        return new VehicleDiagnosticActionOutcome.ValidationFailed(errors);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new VehicleDiagnosticActionOutcome.Forbidden();

                    case HttpStatusCode.NotFound:
                        return new VehicleDiagnosticActionOutcome.NotFound();

                    default:
                        return new VehicleDiagnosticActionOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new VehicleDiagnosticActionOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    public async Task<VehicleDiagnosticsListOutcome> GetListAsync(
        Guid maintenanceRecordId,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(
                $"VehicleDiagnostics?maintenanceRecordId={Uri.EscapeDataString(maintenanceRecordId.ToString())}",
                cancellationToken);
        }
        catch (Exception)
        {
            return new VehicleDiagnosticsListOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var diagnostics = await response.Content
                            .ReadFromJsonAsync<IReadOnlyList<VehicleDiagnosticResponse>>(cancellationToken);
                        return new VehicleDiagnosticsListOutcome.Success(diagnostics ?? []);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new VehicleDiagnosticsListOutcome.Forbidden();

                    case HttpStatusCode.NotFound:
                        return new VehicleDiagnosticsListOutcome.NotFound();

                    default:
                        return new VehicleDiagnosticsListOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new VehicleDiagnosticsListOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }
}
