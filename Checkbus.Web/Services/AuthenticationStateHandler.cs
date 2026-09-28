using System.Net.Http.Headers;
using Checkbus.Web.Extensions;
using Microsoft.AspNetCore.Components.Authorization;

namespace Checkbus.Web.Services;

/// <summary>
/// Forwards the signed-in user's JWT — stored as a custom <c>"access_token"</c> claim, not the
/// standard ASP.NET Core authentication token store — as an <c>Authorization: Bearer</c> header
/// on outgoing requests to the API.
/// </summary>
/// <remarks>
/// <see cref="IHttpClientFactory"/> creates <see cref="DelegatingHandler"/> instances in its own
/// internal DI scope, separate from the Blazor circuit's scope, so this handler cannot safely
/// resolve <see cref="AuthenticationStateProvider"/> via constructor injection — the
/// circuit-scoped instance would never be reachable that way. Instead it reads the circuit's
/// <see cref="IServiceProvider"/> from <see cref="HttpRequestMessage.Options"/>, placed there by
/// <see cref="ApplicationScopeHandler"/> earlier in the pipeline, per the Microsoft-documented
/// "Application scope handler" pattern.
/// </remarks>
public sealed class AuthenticationStateHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Options.TryGetValue(ApplicationScopeHandlerExtensions.ScopeKey, out var scopeServiceProvider))
        {
            var authenticationStateProvider = scopeServiceProvider.GetService<AuthenticationStateProvider>();

            if (authenticationStateProvider is not null)
            {
                var authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();
                var token = authenticationState.User.FindFirst("access_token")?.Value;

                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
