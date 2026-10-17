using System.Net;
using System.Net.Http.Json;
using Checkbus.Web.Features.Notifications.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Web.Features.Notifications.Services;

/// <summary>
/// Typed client for the <c>api/Notifications</c> endpoints (CU-04): publishing a manual
/// notification to a Trip's or an Event's Confirmada passengers, and fetching a Trip's relevant
/// notification history. Maps every realistic server response into the matching closed outcome
/// type so callers never need to inspect HTTP status codes directly. Mirrors
/// <see cref="Checkbus.Web.Features.Trips.Services.TripsClient"/>'s pattern, including taking the
/// keyed <c>"apiservice"</c> <see cref="HttpClient"/> via <c>[FromKeyedServices]</c>.
/// </summary>
public sealed class NotificationsClient([FromKeyedServices("apiservice")] HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<NotificationCreateOutcome> CreateAsync(
        CreateNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("Notifications", request, cancellationToken);
        }
        catch (Exception)
        {
            return new NotificationCreateOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Created:
                        return new NotificationCreateOutcome.Success();

                    case HttpStatusCode.BadRequest:
                        var validationProblem = await response.Content
                            .ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
                        IReadOnlyDictionary<string, string[]> errors = validationProblem is null
                            ? new Dictionary<string, string[]>()
                            : (IReadOnlyDictionary<string, string[]>)validationProblem.Errors;
                        return new NotificationCreateOutcome.ValidationFailed(errors);

                    case HttpStatusCode.NotFound:
                        return new NotificationCreateOutcome.NotFound();

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new NotificationCreateOutcome.Forbidden();

                    default:
                        return new NotificationCreateOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new NotificationCreateOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    public async Task<NotificationHistoryOutcome> GetTripHistoryAsync(
        Guid tripId,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync($"Notifications?tripId={tripId}", cancellationToken);
        }
        catch (Exception)
        {
            return new NotificationHistoryOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var notifications = await response.Content
                            .ReadFromJsonAsync<IReadOnlyList<NotificationResponse>>(cancellationToken);
                        return new NotificationHistoryOutcome.Success(notifications ?? []);

                    case HttpStatusCode.NotFound:
                        return new NotificationHistoryOutcome.NotFound();

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new NotificationHistoryOutcome.Forbidden();

                    default:
                        return new NotificationHistoryOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                return new NotificationHistoryOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }
}
