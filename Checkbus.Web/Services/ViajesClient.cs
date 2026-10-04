using System.Net;
using System.Net.Http.Json;
using Checkbus.Web.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Web.Services;

/// <summary>
/// Typed client for the <c>api/Viajes</c> endpoints: creating a viaje, listing the caller's
/// organization's viajes, and fetching one by id. Maps every realistic server response into the
/// matching closed outcome type so callers never need to inspect HTTP status codes directly.
/// Mirrors <see cref="EventosClient"/>'s pattern, including taking the keyed <c>"apiservice"</c>
/// <see cref="HttpClient"/> via <c>[FromKeyedServices]</c> — see that class's remarks for why.
/// </summary>
public sealed class ViajesClient([FromKeyedServices("apiservice")] HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<ViajeActionOutcome> CreateAsync(
        CreateViajeRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("Viajes", request, cancellationToken);
        }
        catch (Exception)
        {
            return new ViajeActionOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Created:
                        var viaje = await response.Content
                            .ReadFromJsonAsync<ViajeResponse>(cancellationToken);
                        return new ViajeActionOutcome.Success(viaje!);

                    case HttpStatusCode.BadRequest:
                        var validationProblem = await response.Content
                            .ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
                        IReadOnlyDictionary<string, string[]> errors = validationProblem is null
                            ? new Dictionary<string, string[]>()
                            : (IReadOnlyDictionary<string, string[]>)validationProblem.Errors;
                        return new ViajeActionOutcome.ValidationFailed(errors);

                    case HttpStatusCode.NotFound:
                        return new ViajeActionOutcome.NotFound();

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new ViajeActionOutcome.Forbidden();

                    default:
                        return new ViajeActionOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new ViajeActionOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    public async Task<ViajesListOutcome> GetListAsync(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync("Viajes", cancellationToken);
        }
        catch (Exception)
        {
            return new ViajesListOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var viajes = await response.Content
                            .ReadFromJsonAsync<IReadOnlyList<ViajeResponse>>(cancellationToken);
                        return new ViajesListOutcome.Success(viajes ?? []);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new ViajesListOutcome.Forbidden();

                    default:
                        return new ViajesListOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new ViajesListOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    public async Task<ViajeOutcome> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync($"Viajes/{id}", cancellationToken);
        }
        catch (Exception)
        {
            return new ViajeOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var viaje = await response.Content
                            .ReadFromJsonAsync<ViajeDetailResponse>(cancellationToken);
                        return new ViajeOutcome.Success(viaje!);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new ViajeOutcome.Forbidden();

                    case HttpStatusCode.NotFound:
                        return new ViajeOutcome.NotFound();

                    default:
                        return new ViajeOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new ViajeOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }
}
