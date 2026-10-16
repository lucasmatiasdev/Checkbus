using System.Net;
using System.Net.Http.Json;
using Checkbus.Web.Features.Events.Services;
using Checkbus.Web.Features.Trips.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Web.Features.Trips.Services;

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

    public async Task<TripPublishOutcome> PublishAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync($"Trips/{id}/publish", null, cancellationToken);
        }
        catch (Exception)
        {
            return new TripPublishOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.NoContent:
                        return new TripPublishOutcome.Success();

                    case HttpStatusCode.BadRequest:
                        var validationProblem = await response.Content
                            .ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
                        IReadOnlyDictionary<string, string[]> errors = validationProblem is null
                            ? new Dictionary<string, string[]>()
                            : (IReadOnlyDictionary<string, string[]>)validationProblem.Errors;
                        return new TripPublishOutcome.ValidationFailed(errors);

                    case HttpStatusCode.NotFound:
                        return new TripPublishOutcome.NotFound();

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new TripPublishOutcome.Forbidden();

                    default:
                        return new TripPublishOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new TripPublishOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    public async Task<TripStartOutcome> StartAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync($"Trips/{id}/start", null, cancellationToken);
        }
        catch (Exception)
        {
            return new TripStartOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.NoContent:
                        return new TripStartOutcome.Success();

                    case HttpStatusCode.BadRequest:
                        var validationProblem = await response.Content
                            .ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
                        IReadOnlyDictionary<string, string[]> errors = validationProblem is null
                            ? new Dictionary<string, string[]>()
                            : (IReadOnlyDictionary<string, string[]>)validationProblem.Errors;
                        return new TripStartOutcome.ValidationFailed(errors);

                    case HttpStatusCode.NotFound:
                        return new TripStartOutcome.NotFound();

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new TripStartOutcome.Forbidden();

                    default:
                        return new TripStartOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                return new TripStartOutcome.TransportError(
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

    // ---- Fase D3 (odd/tasks/mejoras-detalle-viaje.md) ----

    public async Task<TripStopTimesUpdateOutcome> UpdateStopTimesAsync(
        Guid tripId,
        UpdateStopTimesRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PutAsJsonAsync($"Trips/{tripId}/stops/times", request, cancellationToken);
        }
        catch (Exception)
        {
            return new TripStopTimesUpdateOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.NoContent:
                        return new TripStopTimesUpdateOutcome.Success();

                    case HttpStatusCode.BadRequest:
                        var validationProblem = await response.Content
                            .ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
                        IReadOnlyDictionary<string, string[]> errors = validationProblem is null
                            ? new Dictionary<string, string[]>()
                            : (IReadOnlyDictionary<string, string[]>)validationProblem.Errors;
                        return new TripStopTimesUpdateOutcome.ValidationFailed(errors);

                    case HttpStatusCode.NotFound:
                        return new TripStopTimesUpdateOutcome.NotFound();

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new TripStopTimesUpdateOutcome.Forbidden();

                    default:
                        return new TripStopTimesUpdateOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                return new TripStopTimesUpdateOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    // ---- Fase E1 (odd/tasks/mejoras-detalle-viaje.md) ----

    public async Task<TripSeatMapOutcome> GetSeatMapAsync(
        Guid tripId,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync($"Trips/{tripId}/seats", cancellationToken);
        }
        catch (Exception)
        {
            return new TripSeatMapOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var seatMap = await response.Content
                            .ReadFromJsonAsync<TripSeatMapResponse>(cancellationToken);
                        return new TripSeatMapOutcome.Success(seatMap!);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new TripSeatMapOutcome.Forbidden();

                    case HttpStatusCode.NotFound:
                        return new TripSeatMapOutcome.NotFound();

                    default:
                        return new TripSeatMapOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                return new TripSeatMapOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }
}

/// <summary>
/// Closed outcome of a <c>POST api/Trips/{id}/start</c> call (CU-08). Mirrors
/// <see cref="TripPublishOutcome"/>'s no-body-success shape (204 No Content).
/// </summary>
public abstract record TripStartOutcome
{
    private TripStartOutcome()
    {
    }

    /// <summary>204 No Content — the trip was started successfully.</summary>
    public sealed record Success : TripStartOutcome;

    /// <summary>
    /// 400 Bad Request — the trip is not currently Publicado, or outside the ±30-minute start
    /// window, keyed under "Habilitacion".
    /// </summary>
    public sealed record ValidationFailed(IReadOnlyDictionary<string, string[]> Errors) : TripStartOutcome;

    /// <summary>404 Not Found — the trip doesn't exist or is outside the caller's organization.</summary>
    public sealed record NotFound : TripStartOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : TripStartOutcome;

    /// <summary>Any transport-level failure, short and user-presentable.</summary>
    public sealed record TransportError(string Message) : TripStartOutcome;
}

/// <summary>
/// Closed outcome of a <c>PUT api/Trips/{id}/stops/times</c> call
/// (odd/tasks/mejoras-detalle-viaje.md Fase D3). Mirrors <see cref="TripPublishOutcome"/>'s
/// no-body-success shape (204 No Content). The private constructor confines the hierarchy to
/// the nested cases declared here.
/// </summary>
public abstract record TripStopTimesUpdateOutcome
{
    private TripStopTimesUpdateOutcome()
    {
    }

    /// <summary>204 No Content — the stop times were updated successfully.</summary>
    public sealed record Success : TripStopTimesUpdateOutcome;

    /// <summary>400 Bad Request — FluentValidation failures keyed by property name.</summary>
    public sealed record ValidationFailed(IReadOnlyDictionary<string, string[]> Errors) : TripStopTimesUpdateOutcome;

    /// <summary>
    /// 404 Not Found — the trip doesn't exist, is outside the caller's organization, or a
    /// submitted StopId doesn't belong to this trip's route.
    /// </summary>
    public sealed record NotFound : TripStopTimesUpdateOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : TripStopTimesUpdateOutcome;

    /// <summary>Any transport-level failure, short and user-presentable.</summary>
    public sealed record TransportError(string Message) : TripStopTimesUpdateOutcome;
}

/// <summary>
/// Closed outcome of a <c>GET api/Trips/{id}/seats</c> call
/// (odd/tasks/mejoras-detalle-viaje.md Fase E1). Mirrors <see cref="TripOutcome"/>'s shape.
/// The private constructor confines the hierarchy to the nested cases declared here.
/// </summary>
public abstract record TripSeatMapOutcome
{
    private TripSeatMapOutcome()
    {
    }

    /// <summary>200 OK — the seat map was loaded successfully.</summary>
    public sealed record Success(TripSeatMapResponse SeatMap) : TripSeatMapOutcome;

    /// <summary>404 Not Found — the trip doesn't exist or is outside the caller's organization.</summary>
    public sealed record NotFound : TripSeatMapOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : TripSeatMapOutcome;

    /// <summary>Any transport-level failure, short and user-presentable.</summary>
    public sealed record TransportError(string Message) : TripSeatMapOutcome;
}
