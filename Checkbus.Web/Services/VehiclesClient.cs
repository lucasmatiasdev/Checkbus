using System.Net;
using System.Net.Http.Json;
using Checkbus.Web.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Web.Services;

/// <summary>
/// Typed client for the <c>api/Vehicles</c> endpoints: registering a vehicle and listing the
/// caller's organization's vehicles. Maps every realistic server response into the matching closed
/// outcome type so callers never need to inspect HTTP status codes directly.
/// </summary>
/// <remarks>
/// Takes the keyed <c>"apiservice"</c> <see cref="HttpClient"/> via <c>[FromKeyedServices]</c>
/// rather than <see cref="IHttpClientFactory"/> — see
/// <see cref="Checkbus.Web.Extensions.ApplicationScopeHandlerExtensions"/> for why the plain
/// factory-created client would silently miss the bearer token. Because this class is registered
/// as a normal scoped service (see <c>Program.cs</c>), ASP.NET Core's DI container resolves the
/// keyed parameter automatically — upstream consumers just inject <see cref="VehiclesClient"/>
/// normally.
/// </remarks>
public sealed class VehiclesClient([FromKeyedServices("apiservice")] HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<VehicleRegistrationOutcome> CreateAsync(
        CreateVehicleRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("Vehicles", request, cancellationToken);
        }
        catch (Exception)
        {
            return new VehicleRegistrationOutcome.TransportError(
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
                            .ReadFromJsonAsync<VehicleRegistrationResult>(cancellationToken);
                        return new VehicleRegistrationOutcome.Success(result!);

                    case HttpStatusCode.BadRequest:
                        var validationProblem = await response.Content
                            .ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
                        IReadOnlyDictionary<string, string[]> errors = validationProblem is null
                            ? new Dictionary<string, string[]>()
                            : (IReadOnlyDictionary<string, string[]>)validationProblem.Errors;
                        return new VehicleRegistrationOutcome.ValidationFailed(errors);

                    case HttpStatusCode.Conflict:
                        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
                        return new VehicleRegistrationOutcome.Conflict(
                            problem?.Detail ?? "An unexpected conflict occurred. Please try again.");

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new VehicleRegistrationOutcome.Forbidden();

                    default:
                        return new VehicleRegistrationOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new VehicleRegistrationOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }

    public async Task<VehiclesListOutcome> GetVehiclesAsync(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync("Vehicles", cancellationToken);
        }
        catch (Exception)
        {
            return new VehiclesListOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var vehicles = await response.Content
                            .ReadFromJsonAsync<IReadOnlyList<VehicleResponse>>(cancellationToken);
                        return new VehiclesListOutcome.Success(vehicles ?? []);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new VehiclesListOutcome.Forbidden();

                    default:
                        return new VehiclesListOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new VehiclesListOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }
}
