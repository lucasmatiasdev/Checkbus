using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Checkbus.Web.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Web.Services;

/// <summary>
/// Typed client for the <c>api/DriverRequirements</c> endpoints: listing a user's two driver
/// requirements, uploading a document (multipart), approving/rejecting a document, and fetching
/// the organization-wide expiring/expired count. Maps every realistic server response into the
/// matching closed outcome type so callers never need to inspect HTTP status codes directly.
/// </summary>
/// <remarks>
/// Takes the keyed <c>"apiservice"</c> <see cref="HttpClient"/> via <c>[FromKeyedServices]</c>
/// rather than <see cref="IHttpClientFactory"/> — see
/// <see cref="Checkbus.Web.Extensions.ApplicationScopeHandlerExtensions"/> for why the plain
/// factory-created client would silently miss the bearer token. Because this class is registered
/// as a normal scoped service (see <c>Program.cs</c>), ASP.NET Core's DI container resolves the
/// keyed parameter automatically — upstream consumers just inject
/// <see cref="DriverRequirementsClient"/> normally.
/// </remarks>
public sealed class DriverRequirementsClient([FromKeyedServices("apiservice")] HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<DriverRequirementsListOutcome> GetRequirementsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync($"DriverRequirements/{userId}", cancellationToken);
        }
        catch (Exception)
        {
            return new DriverRequirementsListOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var requirements = await response.Content
                            .ReadFromJsonAsync<IReadOnlyList<DriverRequirementResponse>>(cancellationToken);
                        return new DriverRequirementsListOutcome.Success(requirements ?? []);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new DriverRequirementsListOutcome.Forbidden();

                    case HttpStatusCode.NotFound:
                        return new DriverRequirementsListOutcome.NotFound();

                    default:
                        return new DriverRequirementsListOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new DriverRequirementsListOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    public async Task<DriverRequirementActionOutcome> UploadDocumentAsync(
        Guid userId,
        WebDriverRequirementType type,
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
                $"DriverRequirements/{userId}/{type}/document", multipartContent, cancellationToken);
        }
        catch (Exception)
        {
            return new DriverRequirementActionOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.NoContent:
                        return new DriverRequirementActionOutcome.Success();

                    case HttpStatusCode.BadRequest:
                        var validationProblem = await response.Content
                            .ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
                        IReadOnlyDictionary<string, string[]> errors = validationProblem is null
                            ? new Dictionary<string, string[]>()
                            : (IReadOnlyDictionary<string, string[]>)validationProblem.Errors;
                        return new DriverRequirementActionOutcome.ValidationFailed(errors);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new DriverRequirementActionOutcome.Forbidden();

                    case HttpStatusCode.NotFound:
                        return new DriverRequirementActionOutcome.NotFound();

                    default:
                        return new DriverRequirementActionOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new DriverRequirementActionOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    public async Task<DriverRequirementActionOutcome> ValidateAsync(
        Guid userId,
        WebDriverRequirementType type,
        bool approved,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PutAsJsonAsync(
                $"DriverRequirements/{userId}/{type}/status", new { Approved = approved }, cancellationToken);
        }
        catch (Exception)
        {
            return new DriverRequirementActionOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.NoContent:
                        return new DriverRequirementActionOutcome.Success();

                    case HttpStatusCode.BadRequest:
                        var validationProblem = await response.Content
                            .ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
                        IReadOnlyDictionary<string, string[]> errors = validationProblem is null
                            ? new Dictionary<string, string[]>()
                            : (IReadOnlyDictionary<string, string[]>)validationProblem.Errors;
                        return new DriverRequirementActionOutcome.ValidationFailed(errors);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new DriverRequirementActionOutcome.Forbidden();

                    case HttpStatusCode.NotFound:
                        return new DriverRequirementActionOutcome.NotFound();

                    default:
                        return new DriverRequirementActionOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new DriverRequirementActionOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    public async Task<DriverRequirementCountOutcome> GetExpiringCountAsync(
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync("DriverRequirements/expiring-count", cancellationToken);
        }
        catch (Exception)
        {
            return new DriverRequirementCountOutcome.TransportError(
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
                        return new DriverRequirementCountOutcome.Success(count);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new DriverRequirementCountOutcome.Forbidden();

                    default:
                        return new DriverRequirementCountOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new DriverRequirementCountOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }
}
