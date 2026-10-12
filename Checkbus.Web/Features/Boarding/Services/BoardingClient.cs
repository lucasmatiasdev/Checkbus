using System.Net;
using System.Net.Http.Json;
using Checkbus.Web.Features.Boarding.Contracts;
using Checkbus.Web.Features.Trips.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Web.Features.Boarding.Services;

/// <summary>
/// Typed client for <c>api/boarding</c> (odd/tasks/reservas-publicas.md Fase 3). Reuses the
/// keyed <c>"apiservice"</c> <see cref="HttpClient"/>, same as every other feature client in
/// this codebase — see <c>TripsClient</c>'s remarks for why.
/// </summary>
public sealed class BoardingClient([FromKeyedServices("apiservice")] HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<DriverTripsOutcome> GetMyTripsAsync(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync("boarding/my-trips", cancellationToken);
        }
        catch (Exception)
        {
            return new DriverTripsOutcome.TransportError("An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var trips = await response.Content.ReadFromJsonAsync<IReadOnlyList<TripResponse>>(cancellationToken);
                        return new DriverTripsOutcome.Success(trips ?? []);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new DriverTripsOutcome.Forbidden();

                    default:
                        return new DriverTripsOutcome.TransportError("An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                return new DriverTripsOutcome.TransportError("An unexpected error occurred. Please try again.");
            }
        }
    }

    public async Task<TripBoardingListOutcome> GetBoardingListAsync(Guid tripId, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync($"boarding/{tripId}", cancellationToken);
        }
        catch (Exception)
        {
            return new TripBoardingListOutcome.TransportError("An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var list = await response.Content.ReadFromJsonAsync<TripBoardingListResponse>(cancellationToken);
                        return new TripBoardingListOutcome.Success(list!);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new TripBoardingListOutcome.Forbidden();

                    case HttpStatusCode.NotFound:
                        return new TripBoardingListOutcome.NotFound();

                    default:
                        return new TripBoardingListOutcome.TransportError("An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                return new TripBoardingListOutcome.TransportError("An unexpected error occurred. Please try again.");
            }
        }
    }

    public async Task<BoardingActionOutcome> ValidateAsync(
        Guid reservationId, WebValidationMethod method, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(
                "boarding/validate", new ValidateBoardingRequest(reservationId, method), cancellationToken);
        }
        catch (Exception)
        {
            return new BoardingActionOutcome.TransportError("An unexpected error occurred. Please try again.");
        }

        return await ReadActionOutcomeAsync(response, cancellationToken);
    }

    public async Task<BoardingActionOutcome> CloseBoardingAsync(Guid tripId, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync($"boarding/{tripId}/close", null, cancellationToken);
        }
        catch (Exception)
        {
            return new BoardingActionOutcome.TransportError("An unexpected error occurred. Please try again.");
        }

        return await ReadActionOutcomeAsync(response, cancellationToken);
    }

    private static async Task<BoardingActionOutcome> ReadActionOutcomeAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.NoContent:
                        return new BoardingActionOutcome.Success();

                    case HttpStatusCode.BadRequest:
                        var validationProblem = await response.Content
                            .ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
                        IReadOnlyDictionary<string, string[]> errors = validationProblem is null
                            ? new Dictionary<string, string[]>()
                            : (IReadOnlyDictionary<string, string[]>)validationProblem.Errors;
                        return new BoardingActionOutcome.ValidationFailed(errors);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new BoardingActionOutcome.Forbidden();

                    case HttpStatusCode.NotFound:
                        return new BoardingActionOutcome.NotFound();

                    case HttpStatusCode.Conflict:
                        var conflictProblem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
                        return new BoardingActionOutcome.Conflict(
                            conflictProblem?.Detail ?? "Esta reserva ya fue validada o marcada como ausente.");

                    default:
                        return new BoardingActionOutcome.TransportError("An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                return new BoardingActionOutcome.TransportError("An unexpected error occurred. Please try again.");
            }
        }
    }
}
