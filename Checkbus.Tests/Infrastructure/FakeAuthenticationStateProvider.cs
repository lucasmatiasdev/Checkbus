using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Checkbus.Tests.Infrastructure;

/// <summary>
/// Minimal <see cref="AuthenticationStateProvider"/> test double that returns a canned
/// <see cref="AuthenticationState"/> built from a caller-supplied <see cref="ClaimsPrincipal"/>,
/// optionally carrying an <c>"access_token"</c> claim — used to test outgoing-request
/// middleware that reads the current authentication state.
/// </summary>
internal sealed class FakeAuthenticationStateProvider(ClaimsPrincipal principal) : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        Task.FromResult(new AuthenticationState(principal));
}
