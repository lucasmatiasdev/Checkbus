using System.Net;
using System.Net.Http.Json;
using Checkbus.Web.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Web.Services;

/// <summary>
/// Typed client for the admin-only <c>POST /api/Users</c> registration endpoint. Maps every
/// realistic server response (success, validation failure, conflict, auth failure, transport
/// failure) into a <see cref="UserRegistrationOutcome"/> so callers never need to inspect HTTP
/// status codes directly.
/// </summary>
/// <remarks>
/// Takes the keyed <c>"apiservice"</c> <see cref="HttpClient"/> via <c>[FromKeyedServices]</c>
/// rather than <see cref="IHttpClientFactory"/> — see
/// <see cref="Checkbus.Web.Extensions.ApplicationScopeHandlerExtensions"/> for why the plain
/// factory-created client would silently miss the bearer token. Because this class is registered
/// as a normal scoped service (see <c>Program.cs</c>), ASP.NET Core's DI container resolves the
/// keyed parameter automatically — upstream consumers just inject
/// <see cref="UserRegistrationClient"/> normally.
/// </remarks>
public sealed class UserRegistrationClient([FromKeyedServices("apiservice")] HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<UserRegistrationOutcome> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("Users", request, cancellationToken);
        }
        catch (Exception)
        {
            return new UserRegistrationOutcome.TransportError(
                "No pudimos conectar con el servidor. Probá de nuevo.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Created:
                        var result = await response.Content.ReadFromJsonAsync<RegisterUserResult>(cancellationToken);
                        return new UserRegistrationOutcome.Success(result!);

                    case HttpStatusCode.BadRequest:
                        var validationProblem = await response.Content
                            .ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken);
                        IReadOnlyDictionary<string, string[]> errors = validationProblem is null
                            ? new Dictionary<string, string[]>()
                            : (IReadOnlyDictionary<string, string[]>)validationProblem.Errors;
                        return new UserRegistrationOutcome.ValidationFailed(errors);

                    case HttpStatusCode.Conflict:
                        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
                        return new UserRegistrationOutcome.Conflict(problem?.Detail ?? "Ocurrió un conflicto. Probá de nuevo.");

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new UserRegistrationOutcome.Forbidden();

                    default:
                        return new UserRegistrationOutcome.TransportError(
                            "Ocurrió un error inesperado. Probá de nuevo.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new UserRegistrationOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }
}
