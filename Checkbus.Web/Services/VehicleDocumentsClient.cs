using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Checkbus.Web.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Web.Services;

/// <summary>
/// Typed client for the <c>api/VehicleDocuments</c> endpoints: listing a vehicle's documents,
/// uploading a document (multipart), validating (Apto/NoApto) a document, and fetching the
/// organization-wide expiring/expired count. Maps every realistic server response into the matching
/// closed outcome type so callers never need to inspect HTTP status codes directly. Every action
/// here is Administrador-only server-side — unlike <see cref="DriverRequirementsClient"/>, there is
/// no Chofer self-service branch at all for vehicle documents.
/// </summary>
/// <remarks>
/// Takes the keyed <c>"apiservice"</c> <see cref="HttpClient"/> via <c>[FromKeyedServices]</c>
/// rather than <see cref="IHttpClientFactory"/> — see
/// <see cref="Checkbus.Web.Extensions.ApplicationScopeHandlerExtensions"/> for why the plain
/// factory-created client would silently miss the bearer token. Because this class is registered
/// as a normal scoped service (see <c>Program.cs</c>), ASP.NET Core's DI container resolves the
/// keyed parameter automatically — upstream consumers just inject
/// <see cref="VehicleDocumentsClient"/> normally.
/// </remarks>
public sealed class VehicleDocumentsClient([FromKeyedServices("apiservice")] HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<VehicleDocumentsListOutcome> GetDocumentsAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync($"VehicleDocuments/{vehicleId}", cancellationToken);
        }
        catch (Exception)
        {
            return new VehicleDocumentsListOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var documents = await response.Content
                            .ReadFromJsonAsync<IReadOnlyList<VehicleDocumentResponse>>(cancellationToken);
                        return new VehicleDocumentsListOutcome.Success(documents ?? []);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new VehicleDocumentsListOutcome.Forbidden();

                    case HttpStatusCode.NotFound:
                        return new VehicleDocumentsListOutcome.NotFound();

                    default:
                        return new VehicleDocumentsListOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new VehicleDocumentsListOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    public async Task<VehicleDocumentActionOutcome> UploadDocumentAsync(
        Guid vehicleId,
        WebVehicleDocumentType type,
        Stream fileStream,
        string fileName,
        string contentType,
        DateOnly expirationDate,
        DateOnly? issueDate,
        CancellationToken cancellationToken = default)
    {
        var multipartContent = new MultipartFormDataContent();

        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        multipartContent.Add(fileContent, "File", fileName);

        multipartContent.Add(new StringContent(expirationDate.ToString("yyyy-MM-dd")), "ExpirationDate");
        if (issueDate.HasValue)
            multipartContent.Add(new StringContent(issueDate.Value.ToString("yyyy-MM-dd")), "IssueDate");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync(
                $"VehicleDocuments/{vehicleId}/{type}/document", multipartContent, cancellationToken);
        }
        catch (Exception)
        {
            return new VehicleDocumentActionOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.NoContent:
                        return new VehicleDocumentActionOutcome.Success();

                    case HttpStatusCode.BadRequest:
                        var validationProblem = await response.Content
                            .ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
                        IReadOnlyDictionary<string, string[]> errors = validationProblem is null
                            ? new Dictionary<string, string[]>()
                            : (IReadOnlyDictionary<string, string[]>)validationProblem.Errors;
                        return new VehicleDocumentActionOutcome.ValidationFailed(errors);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new VehicleDocumentActionOutcome.Forbidden();

                    case HttpStatusCode.NotFound:
                        return new VehicleDocumentActionOutcome.NotFound();

                    default:
                        return new VehicleDocumentActionOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new VehicleDocumentActionOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    /// <summary>
    /// A 404 here can mean either the vehicle doesn't exist/is out-of-org
    /// (<c>VehicleNotFoundException</c>) or this document type was never uploaded
    /// (<c>VehicleDocumentNotFoundException</c>) — the API collapses both to a plain 404 with no
    /// body distinction, so <see cref="VehicleDocumentActionOutcome.NotFound"/> covers both; there
    /// is nothing in the response to distinguish them by.
    /// </summary>
    public async Task<VehicleDocumentActionOutcome> ValidateAsync(
        Guid vehicleId,
        WebVehicleDocumentType type,
        bool approved,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PutAsJsonAsync(
                $"VehicleDocuments/{vehicleId}/{type}/status", new { Approved = approved }, cancellationToken);
        }
        catch (Exception)
        {
            return new VehicleDocumentActionOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.NoContent:
                        return new VehicleDocumentActionOutcome.Success();

                    case HttpStatusCode.BadRequest:
                        var validationProblem = await response.Content
                            .ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
                        IReadOnlyDictionary<string, string[]> errors = validationProblem is null
                            ? new Dictionary<string, string[]>()
                            : (IReadOnlyDictionary<string, string[]>)validationProblem.Errors;
                        return new VehicleDocumentActionOutcome.ValidationFailed(errors);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new VehicleDocumentActionOutcome.Forbidden();

                    case HttpStatusCode.NotFound:
                        return new VehicleDocumentActionOutcome.NotFound();

                    default:
                        return new VehicleDocumentActionOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new VehicleDocumentActionOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    public async Task<VehicleDocumentCountOutcome> GetExpiringCountAsync(
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync("VehicleDocuments/expiring-count", cancellationToken);
        }
        catch (Exception)
        {
            return new VehicleDocumentCountOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var count = await response.Content.ReadFromJsonAsync<int>(cancellationToken);
                        return new VehicleDocumentCountOutcome.Success(count);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new VehicleDocumentCountOutcome.Forbidden();

                    default:
                        return new VehicleDocumentCountOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new VehicleDocumentCountOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }
}
