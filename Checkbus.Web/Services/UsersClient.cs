using System.Net;
using System.Net.Http.Json;
using Checkbus.Web.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Web.Services;

/// <summary>
/// Typed client for the admin-only <c>GET /api/Users</c> listing endpoint. Maps every realistic
/// server response (success, auth failure, transport failure) into a <see cref="UsersListOutcome"/>
/// so callers never need to inspect HTTP status codes directly. Kept as a separate class from
/// <see cref="UserRegistrationClient"/> because the two clients serve different responsibilities
/// (reading the list vs. writing a registration) even though they share the same wiring pattern.
/// </summary>
/// <remarks>
/// Takes the keyed <c>"apiservice"</c> <see cref="HttpClient"/> via <c>[FromKeyedServices]</c>
/// rather than <see cref="IHttpClientFactory"/> — see
/// <see cref="Checkbus.Web.Extensions.ApplicationScopeHandlerExtensions"/> for why the plain
/// factory-created client would silently miss the bearer token. Because this class is registered
/// as a normal scoped service (see <c>Program.cs</c>), ASP.NET Core's DI container resolves the
/// keyed parameter automatically — upstream consumers just inject <see cref="UsersClient"/>
/// normally.
/// </remarks>
public sealed class UsersClient([FromKeyedServices("apiservice")] HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<UsersListOutcome> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync("Users", cancellationToken);
        }
        catch (Exception)
        {
            return new UsersListOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var users = await response.Content
                            .ReadFromJsonAsync<IReadOnlyList<UserListItemResponse>>(cancellationToken);
                        return new UsersListOutcome.Success(users ?? []);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new UsersListOutcome.Forbidden();

                    default:
                        return new UsersListOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new UsersListOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }
}
