using System.Net;
using System.Net.Http.Json;
using Checkbus.Web.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Web.Services;

/// <summary>
/// Typed client for the <c>api/Trips</c> endpoints: creating a trip, listing the caller's
/// organization's trips, and fetching one by id. Maps every realistic server response into the
/// matching closed outcome type so callers never need to inspect HTTP status codes directly.
/// Mirrors <see cref="EventsClient"/>'s pattern, including taking the keyed <c>"apiservice"</c>
/// <see cref="HttpClient"/> via <c>[FromKeyedServices]</c> — see that class's remarks for why.
/// </summary>
public sealed class TripsClient([FromKeyedServices("apiservice")] HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<TripActionOutcome> CreateAsync(
        CreateTripRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("Trips", request, cancellationToken);
        }
        catch (Exception)
        {
            return new TripActionOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Created:
                        var trip = await response.Content
                            .ReadFromJsonAsync<TripResponse>(cancellationToken);
                        return new TripActionOutcome.Success(trip!);

                    case HttpStatusCode.BadRequest:
                        var validationProblem = await response.Content
                            .ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
                        IReadOnlyDictionary<string, string[]> errors = validationProblem is null
                            ? new Dictionary<string, string[]>()
                            : (IReadOnlyDictionary<string, string[]>)validationProblem.Errors;
                        return new TripActionOutcome.ValidationFailed(errors);

                    case HttpStatusCode.NotFound:
                        return new TripActionOutcome.NotFound();

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new TripActionOutcome.Forbidden();

                    default:
                        return new TripActionOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new TripActionOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    public async Task<TripsListOutcome> GetListAsync(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync("Trips", cancellationToken);
        }
        catch (Exception)
        {
            return new TripsListOutcome.TransportError(
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
                            .ReadFromJsonAsync<IReadOnlyList<TripResponse>>(cancellationToken);
                        return new TripsListOutcome.Success(trips ?? []);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new TripsListOutcome.Forbidden();

                    default:
                        return new TripsListOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new TripsListOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    public async Task<TripOutcome> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync($"Trips/{id}", cancellationToken);
        }
        catch (Exception)
        {
            return new TripOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var trip = await response.Content
                            .ReadFromJsonAsync<TripDetailResponse>(cancellationToken);
                        return new TripOutcome.Success(trip!);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new TripOutcome.Forbidden();

                    case HttpStatusCode.NotFound:
                        return new TripOutcome.NotFound();

                    default:
                        return new TripOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new TripOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }
}
