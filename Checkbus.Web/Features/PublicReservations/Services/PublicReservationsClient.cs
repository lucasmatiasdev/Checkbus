using System.Net;
using System.Net.Http.Json;
using Checkbus.Web.Features.PublicReservations.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Web.Features.PublicReservations.Services;

/// <summary>
/// Typed client for <c>api/public/reservations</c> (odd/tasks/reservas-publicas.md R5) — a
/// sibling of <see cref="PublicTripsClient"/>, matching the API's own controller split
/// (<c>PublicReservationsController</c> is not scoped by a single TripId route segment, unlike
/// <c>PublicTripsController</c>'s actions). Reuses the same keyed <c>"apiservice"</c>
/// <see cref="HttpClient"/> for the same reason PublicTripsClient does — see its doc comment.
/// </summary>
public sealed class PublicReservationsClient([FromKeyedServices("apiservice")] HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<ReservationsConfirmationOutcome> GetConfirmationAsync(
        IReadOnlyList<Guid> reservationIds,
        CancellationToken cancellationToken = default)
    {
        // The API model-binds IReadOnlyList<Guid> from repeated "ids=" query keys, not a
        // single comma-joined value — a comma-joined "ids=a,b" fails binding with a 400.
        var query = string.Join("&", reservationIds.Select(id => $"ids={id}"));
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync($"public/reservations?{query}", cancellationToken);
        }
        catch (Exception)
        {
            return new ReservationsConfirmationOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var confirmation = await response.Content
                            .ReadFromJsonAsync<ReservationsConfirmationResponse>(cancellationToken);
                        return new ReservationsConfirmationOutcome.Success(confirmation!);

                    case HttpStatusCode.NotFound:
                        return new ReservationsConfirmationOutcome.NotFound();

                    default:
                        return new ReservationsConfirmationOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not
                // bubble up as an unhandled exception — the caller only ever expects a typed
                // outcome.
                return new ReservationsConfirmationOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    // POST api/public/reservations/{id}/cancel (odd/tasks/reservas-publicas.md Fase 4 / C5) —
    // no request body, the Reservation id comes from the route, same shape GetConfirmation's
    // simplicity suggested rather than inventing a body for a parameterless action.
    public async Task<CancelReservationOutcome> CancelReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync(
                $"public/reservations/{reservationId}/cancel", null, cancellationToken);
        }
        catch (Exception)
        {
            return new CancelReservationOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.NoContent:
                        return new CancelReservationOutcome.Success();

                    case HttpStatusCode.Conflict:
                        var conflictProblem = await response.Content
                            .ReadFromJsonAsync<ProblemDetails>(cancellationToken);
                        return new CancelReservationOutcome.NotCancellable(
                            conflictProblem?.Detail
                                ?? "No se puede cancelar esta reserva.");

                    case HttpStatusCode.NotFound:
                        return new CancelReservationOutcome.NotFound();

                    default:
                        return new CancelReservationOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body must not bubble up as an
                // unhandled exception — the caller only ever expects a typed outcome.
                return new CancelReservationOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }
}
