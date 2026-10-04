using System.Net;
using System.Net.Http.Json;
using Checkbus.Web.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Web.Services;

/// <summary>
/// Typed client for the <c>api/Eventos</c> endpoints: creating an evento and listing the shared
/// catalog of eventos. Maps every realistic server response into the matching closed outcome type
/// so callers never need to inspect HTTP status codes directly. Mirrors
/// <see cref="MaintenanceRecordsClient"/>'s pattern, including taking the keyed <c>"apiservice"</c>
/// <see cref="HttpClient"/> via <c>[FromKeyedServices]</c> — see that class's remarks for why.
/// </summary>
public sealed class EventosClient([FromKeyedServices("apiservice")] HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<EventoActionOutcome> CreateAsync(
        CreateEventoRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("Eventos", request, cancellationToken);
        }
        catch (Exception)
        {
            return new EventoActionOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Created:
                        var evento = await response.Content
                            .ReadFromJsonAsync<EventoResponse>(cancellationToken);
                        return new EventoActionOutcome.Success(evento!);

                    case HttpStatusCode.BadRequest:
                        var validationProblem = await response.Content
                            .ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
                        IReadOnlyDictionary<string, string[]> errors = validationProblem is null
                            ? new Dictionary<string, string[]>()
                            : (IReadOnlyDictionary<string, string[]>)validationProblem.Errors;
                        return new EventoActionOutcome.ValidationFailed(errors);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new EventoActionOutcome.Forbidden();

                    default:
                        return new EventoActionOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new EventoActionOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    public async Task<EventosListOutcome> GetListAsync(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync("Eventos", cancellationToken);
        }
        catch (Exception)
        {
            return new EventosListOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var eventos = await response.Content
                            .ReadFromJsonAsync<IReadOnlyList<EventoResponse>>(cancellationToken);
                        return new EventosListOutcome.Success(eventos ?? []);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new EventosListOutcome.Forbidden();

                    default:
                        return new EventosListOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new EventosListOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }
}
