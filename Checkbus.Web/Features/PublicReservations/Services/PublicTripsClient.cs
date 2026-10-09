using System.Net;
using System.Net.Http.Json;
using Checkbus.Web.Features.PublicReservations.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Web.Features.PublicReservations.Services;

/// <summary>
/// Typed client for the project's first unauthenticated business-domain surface,
/// <c>api/public/trips</c> (odd/tasks/reservas-publicas.md R3). Reuses the same keyed
/// <c>"apiservice"</c> <see cref="HttpClient"/> as <see cref="Checkbus.Web.Features.Trips.Services.TripsClient"/>
/// rather than a separate client: <see cref="Checkbus.Web.Shared.Services.AuthenticationStateHandler"/>
/// already gracefully no-ops when there is no authenticated principal — it only sets the
/// Authorization header when an <c>"access_token"</c> claim is present on
/// <c>AuthenticationStateProvider</c>'s current user, which an anonymous visitor on this
/// public page never has — so no bearer token is ever attached here, confirmed by reading
/// that handler before deciding (see its doc comment).
/// </summary>
public sealed class PublicTripsClient([FromKeyedServices("apiservice")] HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<PublicTripSeatMapOutcome> GetSeatMapAsync(
        Guid tripId,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync($"public/trips/{tripId}/seatmap", cancellationToken);
        }
        catch (Exception)
        {
            return new PublicTripSeatMapOutcome.TransportError(
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
                            .ReadFromJsonAsync<PublicTripSeatMapResponse>(cancellationToken);
                        return new PublicTripSeatMapOutcome.Success(seatMap!);

                    case HttpStatusCode.NotFound:
                        return new PublicTripSeatMapOutcome.NotFound();

                    default:
                        return new PublicTripSeatMapOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new PublicTripSeatMapOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    public async Task<PublicTripDetailOutcome> GetDetailsAsync(
        Guid tripId,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync($"public/trips/{tripId}/details", cancellationToken);
        }
        catch (Exception)
        {
            return new PublicTripDetailOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var detail = await response.Content
                            .ReadFromJsonAsync<PublicTripDetailResponse>(cancellationToken);
                        return new PublicTripDetailOutcome.Success(detail!);

                    case HttpStatusCode.NotFound:
                        return new PublicTripDetailOutcome.NotFound();

                    default:
                        return new PublicTripDetailOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new PublicTripDetailOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    public async Task<CreateReservationOutcome> CreateReservationAsync(
        Guid tripId,
        CreateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(
                $"public/trips/{tripId}/reservations", request, cancellationToken);
        }
        catch (Exception)
        {
            return new CreateReservationOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Created:
                        var result = await response.Content
                            .ReadFromJsonAsync<CreateReservationResponse>(cancellationToken);
                        return new CreateReservationOutcome.Success(result!);

                    case HttpStatusCode.BadRequest:
                        var validationProblem = await response.Content
                            .ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
                        IReadOnlyDictionary<string, string[]> errors = validationProblem is null
                            ? new Dictionary<string, string[]>()
                            : (IReadOnlyDictionary<string, string[]>)validationProblem.Errors;
                        return new CreateReservationOutcome.ValidationFailed(errors);

                    case HttpStatusCode.Conflict:
                        var conflictProblem = await response.Content
                            .ReadFromJsonAsync<ProblemDetails>(cancellationToken);
                        return new CreateReservationOutcome.Conflict(
                            conflictProblem?.Detail
                                ?? "Uno de los asientos elegidos ya fue reservado por otra persona.");

                    case HttpStatusCode.NotFound:
                        return new CreateReservationOutcome.NotFound();

                    default:
                        return new CreateReservationOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new CreateReservationOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }
}
