using System.Net;
using System.Net.Http.Json;
using Checkbus.Web.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Web.Services;

/// <summary>
/// Typed client for the <c>api/MaintenanceRecords</c> endpoints: creating a record, updating its
/// lifecycle fields, listing the caller's organization's records (optionally filtered by
/// vehicle/status/type), and fetching a single record. Maps every realistic server response into
/// the matching closed outcome type so callers never need to inspect HTTP status codes directly.
/// </summary>
/// <remarks>
/// Takes the keyed <c>"apiservice"</c> <see cref="HttpClient"/> via <c>[FromKeyedServices]</c>
/// rather than <see cref="IHttpClientFactory"/> — see
/// <see cref="Checkbus.Web.Extensions.ApplicationScopeHandlerExtensions"/> for why the plain
/// factory-created client would silently miss the bearer token. Because this class is registered
/// as a normal scoped service (see <c>Program.cs</c>), ASP.NET Core's DI container resolves the
/// keyed parameter automatically — upstream consumers just inject
/// <see cref="MaintenanceRecordsClient"/> normally.
/// </remarks>
public sealed class MaintenanceRecordsClient([FromKeyedServices("apiservice")] HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<MaintenanceRecordActionOutcome> CreateAsync(
        CreateMaintenanceRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("MaintenanceRecords", request, cancellationToken);
        }
        catch (Exception)
        {
            return new MaintenanceRecordActionOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Created:
                        var record = await response.Content
                            .ReadFromJsonAsync<MaintenanceRecordResponse>(cancellationToken);
                        return new MaintenanceRecordActionOutcome.Success(record!);

                    case HttpStatusCode.BadRequest:
                        var validationProblem = await response.Content
                            .ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
                        IReadOnlyDictionary<string, string[]> errors = validationProblem is null
                            ? new Dictionary<string, string[]>()
                            : (IReadOnlyDictionary<string, string[]>)validationProblem.Errors;
                        return new MaintenanceRecordActionOutcome.ValidationFailed(errors);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new MaintenanceRecordActionOutcome.Forbidden();

                    case HttpStatusCode.NotFound:
                        return new MaintenanceRecordActionOutcome.NotFound();

                    default:
                        return new MaintenanceRecordActionOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new MaintenanceRecordActionOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    public async Task<MaintenanceRecordUpdateOutcome> UpdateAsync(
        Guid id,
        UpdateMaintenanceRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PutAsJsonAsync($"MaintenanceRecords/{id}", request, cancellationToken);
        }
        catch (Exception)
        {
            return new MaintenanceRecordUpdateOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.NoContent:
                        return new MaintenanceRecordUpdateOutcome.Success();

                    case HttpStatusCode.BadRequest:
                        var validationProblem = await response.Content
                            .ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
                        IReadOnlyDictionary<string, string[]> errors = validationProblem is null
                            ? new Dictionary<string, string[]>()
                            : (IReadOnlyDictionary<string, string[]>)validationProblem.Errors;
                        return new MaintenanceRecordUpdateOutcome.ValidationFailed(errors);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new MaintenanceRecordUpdateOutcome.Forbidden();

                    case HttpStatusCode.NotFound:
                        return new MaintenanceRecordUpdateOutcome.NotFound();

                    default:
                        return new MaintenanceRecordUpdateOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new MaintenanceRecordUpdateOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    public async Task<MaintenanceRecordsListOutcome> GetListAsync(
        Guid? vehicleId = null,
        WebMaintenanceStatus? status = null,
        WebMaintenanceType? type = null,
        CancellationToken cancellationToken = default)
    {
        var query = BuildListQuery(vehicleId, status, type);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync($"MaintenanceRecords{query}", cancellationToken);
        }
        catch (Exception)
        {
            return new MaintenanceRecordsListOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var records = await response.Content
                            .ReadFromJsonAsync<IReadOnlyList<MaintenanceRecordResponse>>(cancellationToken);
                        return new MaintenanceRecordsListOutcome.Success(records ?? []);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new MaintenanceRecordsListOutcome.Forbidden();

                    default:
                        return new MaintenanceRecordsListOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new MaintenanceRecordsListOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    public async Task<MaintenanceRecordOutcome> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync($"MaintenanceRecords/{id}", cancellationToken);
        }
        catch (Exception)
        {
            return new MaintenanceRecordOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var record = await response.Content
                            .ReadFromJsonAsync<MaintenanceRecordResponse>(cancellationToken);
                        return new MaintenanceRecordOutcome.Success(record!);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new MaintenanceRecordOutcome.Forbidden();

                    case HttpStatusCode.NotFound:
                        return new MaintenanceRecordOutcome.NotFound();

                    default:
                        return new MaintenanceRecordOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new MaintenanceRecordOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    /// <summary>
    /// Builds the optional query string for <see cref="GetListAsync"/> — every filter maps to
    /// <c>GetMaintenanceRecordsQuery</c>'s matching nullable property, and an absent filter is
    /// simply omitted rather than sent as an empty value.
    /// </summary>
    private static string BuildListQuery(Guid? vehicleId, WebMaintenanceStatus? status, WebMaintenanceType? type)
    {
        var parts = new List<string>();
        if (vehicleId.HasValue)
            parts.Add($"vehicleId={Uri.EscapeDataString(vehicleId.Value.ToString())}");
        if (status.HasValue)
            parts.Add($"status={(int)status.Value}");
        if (type.HasValue)
            parts.Add($"type={(int)type.Value}");

        return parts.Count == 0 ? string.Empty : $"?{string.Join("&", parts)}";
    }
}
