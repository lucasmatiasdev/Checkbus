using System.Net;
using System.Net.Http.Json;
using Checkbus.Web.Features.Users.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Web.Features.Users.Services;

/// <summary>
/// Typed client for the CheckbusSoporte-only <c>GET /api/users/by-organization/{organizationId}</c>
/// endpoint (CU-16, <c>odd/tasks/auditoria-sistema.md</c> T4) — the users of a specific
/// organization chosen on the "Auditoría" page, for the [Usuario] dropdown's cascade once a
/// CheckbusSoporte caller picks a value in [Organización]. Reuses <see cref="UsersListOutcome"/>
/// and <see cref="UserListItemResponse"/> as-is: the response shape is identical to
/// <see cref="UsersClient.GetUsersAsync"/>'s, only the target organization differs (the caller's
/// own vs. an arbitrary one being browsed) — same anti-IDOR split as the real
/// <c>GetUsersByOrganizationQuery</c>/<c>GetUsersQuery</c> pair on the API side. Kept as a
/// separate class from <see cref="UsersClient"/> rather than an added method there, so this
/// task's new HTTP client stays its own file.
/// </summary>
/// <remarks>
/// Takes the keyed <c>"apiservice"</c> <see cref="HttpClient"/> via <c>[FromKeyedServices]</c>
/// rather than <see cref="IHttpClientFactory"/> — see
/// <see cref="Checkbus.Web.Extensions.ApplicationScopeHandlerExtensions"/> for why the plain
/// factory-created client would silently miss the bearer token. Because this class is registered
/// as a normal scoped service (see <c>Program.cs</c>), ASP.NET Core's DI container resolves the
/// keyed parameter automatically — upstream consumers just inject
/// <see cref="UsersByOrganizationClient"/> normally.
/// </remarks>
public sealed class UsersByOrganizationClient([FromKeyedServices("apiservice")] HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<UsersListOutcome> GetUsersByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync($"users/by-organization/{organizationId}", cancellationToken);
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
