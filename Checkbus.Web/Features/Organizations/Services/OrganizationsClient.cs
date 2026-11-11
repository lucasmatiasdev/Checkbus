using System.Net;
using System.Net.Http.Json;
using Checkbus.Web.Features.Organizations.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Web.Features.Organizations.Services;

/// <summary>
/// Typed client for the CheckbusSoporte-only <c>GET /api/organizations</c> listing endpoint (CU-16,
/// <c>odd/tasks/auditoria-sistema.md</c> T4) — every organization, for the "Auditoría" page's
/// [Organización] dropdown. Maps every realistic server response into an
/// <see cref="OrganizationsListOutcome"/> so callers never need to inspect HTTP status codes
/// directly. Kept as a separate class from <see cref="OrganizationRegistrationClient"/> because
/// the two clients serve different responsibilities (reading the list vs. writing a new
/// registration) even though they share the same wiring pattern.
/// </summary>
/// <remarks>
/// Takes the keyed <c>"apiservice"</c> <see cref="HttpClient"/> via <c>[FromKeyedServices]</c>
/// rather than <see cref="IHttpClientFactory"/> — see
/// <see cref="Checkbus.Web.Extensions.ApplicationScopeHandlerExtensions"/> for why the plain
/// factory-created client would silently miss the bearer token. Because this class is registered
/// as a normal scoped service (see <c>Program.cs</c>), ASP.NET Core's DI container resolves the
/// keyed parameter automatically — upstream consumers just inject <see cref="OrganizationsClient"/>
/// normally.
/// </remarks>
public sealed class OrganizationsClient([FromKeyedServices("apiservice")] HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<OrganizationsListOutcome> GetAllAsync(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync("Organizations", cancellationToken);
        }
        catch (Exception)
        {
            return new OrganizationsListOutcome.TransportError(
                "An unexpected error occurred. Please try again.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var organizations = await response.Content
                            .ReadFromJsonAsync<IReadOnlyList<OrganizationListItemResponse>>(cancellationToken);
                        return new OrganizationsListOutcome.Success(organizations ?? []);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new OrganizationsListOutcome.Forbidden();

                    default:
                        return new OrganizationsListOutcome.TransportError(
                            "An unexpected error occurred. Please try again.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body (e.g. not valid JSON) must not bubble
                // up as an unhandled exception — the caller only ever expects a typed outcome.
                return new OrganizationsListOutcome.TransportError(
                    "An unexpected error occurred. Please try again.");
            }
        }
    }
}
