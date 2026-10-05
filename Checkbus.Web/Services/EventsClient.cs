using System.Net;
using System.Net.Http.Json;
using Checkbus.Web.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Web.Services;

/// <summary>
/// Typed client for the <c>api/Events</c> endpoints: creating an event and listing the shared
/// catalog of events. Maps every realistic server response into the matching closed outcome type
/// so callers never need to inspect HTTP status codes directly. Mirrors
/// <see cref="MaintenanceRecordsClient"/>'s pattern, including taking the keyed <c>"apiservice"</c>
/// <see cref="HttpClient"/> via <c>[FromKeyedServices]</c> — see that class's remarks for why.
/// </summary>
public sealed class EventsClient([FromKeyedServices("apiservice")] HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<EventActionOutcome> CreateAsync(
        CreateEventRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("Events", request, cancellationToken);
        }
        catch (Exception)
        {
            return new EventActionOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Created:
                        var @event = await response.Content
                            .ReadFromJsonAsync<EventResponse>(cancellationToken);
                        return new EventActionOutcome.Success(@event!);

                    case HttpStatusCode.BadRequest:
                        var validationProblem = await response.Content
                            .ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
                        IReadOnlyDictionary<string, string[]> errors = validationProblem is null
                            ? new Dictionary<string, string[]>()
                            : (IReadOnlyDictionary<string, string[]>)validationProblem.Errors;
                        return new EventActionOutcome.ValidationFailed(errors);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new EventActionOutcome.Forbidden();

                    default:
                        return new EventActionOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new EventActionOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    public async Task<EventsListOutcome> GetListAsync(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync("Events", cancellationToken);
        }
        catch (Exception)
        {
            return new EventsListOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var events = await response.Content
                            .ReadFromJsonAsync<IReadOnlyList<EventResponse>>(cancellationToken);
                        return new EventsListOutcome.Success(events ?? []);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new EventsListOutcome.Forbidden();

                    default:
                        return new EventsListOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new EventsListOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }
}
