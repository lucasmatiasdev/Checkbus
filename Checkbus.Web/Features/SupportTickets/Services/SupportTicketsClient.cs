using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Checkbus.Web.Features.SupportTickets.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Web.Features.SupportTickets.Services;

/// <summary>
/// Typed client for the <c>api/SupportTickets</c> endpoints: listing the caller's organization's
/// tickets and creating a new one (multipart, with an optional single attachment). Maps every
/// realistic server response into the matching closed outcome type so callers never need to
/// inspect HTTP status codes directly. Every action here is Administrador/Planificador-only
/// server-side, same as <see cref="Checkbus.Web.Features.VehicleDocuments.Services.VehicleDocumentsClient"/>.
/// </summary>
/// <remarks>
/// Takes the keyed <c>"apiservice"</c> <see cref="HttpClient"/> via <c>[FromKeyedServices]</c>
/// rather than <see cref="IHttpClientFactory"/> — see
/// <see cref="Checkbus.Web.Extensions.ApplicationScopeHandlerExtensions"/> for why the plain
/// factory-created client would silently miss the bearer token. Because this class is registered
/// as a normal scoped service (see <c>Program.cs</c>), ASP.NET Core's DI container resolves the
/// keyed parameter automatically — upstream consumers just inject
/// <see cref="SupportTicketsClient"/> normally.
/// </remarks>
public sealed class SupportTicketsClient([FromKeyedServices("apiservice")] HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<SupportTicketsListOutcome> GetMyTicketsAsync(
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync("SupportTickets", cancellationToken);
        }
        catch (Exception)
        {
            return new SupportTicketsListOutcome.TransportError(
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
                            .ReadFromJsonAsync<IReadOnlyList<SupportTicketResponse>>(cancellationToken);
                        return new SupportTicketsListOutcome.Success(tickets ?? []);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new SupportTicketsListOutcome.Forbidden();

                    default:
                        return new SupportTicketsListOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new SupportTicketsListOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    /// <summary>
    /// Non-blocking duplicate hint: returns whether the caller's organization already has an
    /// open ticket in this category created within the last 24 hours. Always resolves to
    /// <c>false</c> on any transport/parsing failure — this is a UX hint, never a gate on ticket
    /// creation, so a failed check must stay silent rather than surface an error.
    /// </summary>
    public async Task<bool> CheckDuplicateAsync(
        WebSupportTicketCategory category, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"SupportTickets/check-duplicate?category={category}", cancellationToken);

            using (response)
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return false;
                }

                return await response.Content.ReadFromJsonAsync<bool>(cancellationToken);
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<SupportTicketCreateOutcome> CreateTicketAsync(
        WebSupportTicketCategory category,
        string subject,
        string description,
        Stream? attachmentStream,
        string? attachmentFileName,
        string? attachmentContentType,
        CancellationToken cancellationToken = default)
    {
        var multipartContent = new MultipartFormDataContent
        {
            { new StringContent(category.ToString()), "Category" },
            { new StringContent(subject), "Subject" },
            { new StringContent(description), "Description" }
        };

        // The attachment is optional — only attach a file part when the caller actually picked
        // one, same guard VehicleDocumentsClient doesn't need (its file is required) but
        // CreateSupportTicketRequest.Attachment is nullable.
        if (attachmentStream is not null)
        {
            var fileContent = new StreamContent(attachmentStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                attachmentContentType ?? "application/octet-stream");
            multipartContent.Add(fileContent, "Attachment", attachmentFileName ?? "attachment");
        }

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync("SupportTickets", multipartContent, cancellationToken);
        }
        catch (Exception)
        {
            return new SupportTicketCreateOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Created:
                        var ticket = await response.Content
                            .ReadFromJsonAsync<SupportTicketResponse>(cancellationToken);
                        return ticket is null
                            ? new SupportTicketCreateOutcome.TransportError(
                                "An unexpected error occurred. Please try again.")
                            : new SupportTicketCreateOutcome.Success(ticket);

                    case HttpStatusCode.BadRequest:
                        var validationProblem = await response.Content
                            .ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
                        IReadOnlyDictionary<string, string[]> errors = validationProblem is null
                            ? new Dictionary<string, string[]>()
                            : (IReadOnlyDictionary<string, string[]>)validationProblem.Errors;
                        return new SupportTicketCreateOutcome.ValidationFailed(errors);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new SupportTicketCreateOutcome.Forbidden();

                    default:
                        return new SupportTicketCreateOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new SupportTicketCreateOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }
}
