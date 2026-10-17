using System.Net;
using System.Net.Http.Json;
using Checkbus.Web.Features.Organizations.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Web.Features.Organizations.Services;

/// <summary>
/// Typed client for the CheckbusSoporte-only <c>POST /api/Organizations</c> registration
/// endpoint. Maps every realistic server response (success, validation failure, conflict, auth
/// failure, transport failure) into an <see cref="OrganizationRegistrationOutcome"/> so callers
/// never need to inspect HTTP status codes directly. Mirrors <c>UserRegistrationClient</c>
/// exactly.
/// </summary>
/// <remarks>
/// Takes the keyed <c>"apiservice"</c> <see cref="HttpClient"/> via <c>[FromKeyedServices]</c>
/// rather than <see cref="IHttpClientFactory"/> — see
/// <see cref="Checkbus.Web.Extensions.ApplicationScopeHandlerExtensions"/> for why the plain
/// factory-created client would silently miss the bearer token. Because this class is registered
/// as a normal scoped service (see <c>Program.cs</c>), ASP.NET Core's DI container resolves the
/// keyed parameter automatically — upstream consumers just inject
/// <see cref="OrganizationRegistrationClient"/> normally.
/// </remarks>
public sealed class OrganizationRegistrationClient([FromKeyedServices("apiservice")] HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<OrganizationRegistrationOutcome> RegisterAsync(
        RegisterOrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("Organizations", request, cancellationToken);
        }
        catch (Exception)
        {
            return new OrganizationRegistrationOutcome.TransportError(
                "No pudimos conectar con el servidor. Probá de nuevo.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Created:
                        var result = await response.Content.ReadFromJsonAsync<RegisterOrganizationResult>(cancellationToken);
                        return new OrganizationRegistrationOutcome.Success(result!);

                    case HttpStatusCode.BadRequest:
                        var validationProblem = await response.Content
                            .ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
                        IReadOnlyDictionary<string, string[]> errors = validationProblem is null
                            ? new Dictionary<string, string[]>()
                            : (IReadOnlyDictionary<string, string[]>)validationProblem.Errors;
                        return new OrganizationRegistrationOutcome.ValidationFailed(errors);

                    case HttpStatusCode.Conflict:
                        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
                        return new OrganizationRegistrationOutcome.Conflict(problem?.Detail ?? "Ocurrió un conflicto. Probá de nuevo.");

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new OrganizationRegistrationOutcome.Forbidden();

                    default:
                        return new OrganizationRegistrationOutcome.TransportError(
                            "Ocurrió un error inesperado. Probá de nuevo.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new OrganizationRegistrationOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }
}
