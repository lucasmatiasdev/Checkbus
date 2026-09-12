using Checkbus.Application.Abstractions;
using Microsoft.AspNetCore.Components.Authorization;

namespace Checkbus.Presentation.Authentication
{
    /// <summary>
    /// Scoped <see cref="ICurrentTenant"/> that synchronously projects the current Blazor Server
    /// circuit's authenticated tenant (see design D1). Primed once from
    /// <see cref="AuthenticationStateProvider"/> via <see cref="PrimeAsync"/> (called from
    /// <c>MainLayout.OnInitializedAsync</c>) and kept in sync through
    /// <see cref="AuthenticationStateProvider.AuthenticationStateChanged"/>, so EF Core's global
    /// query filter closure (a synchronous delegate) can read a cached value without an
    /// additional round trip. Deliberately does NOT use <c>IHttpContextAccessor</c>: it is null
    /// after the first render inside a Blazor Server circuit and would risk leaking a stale or
    /// cross-tenant value. Must be registered Scoped so each circuit gets its own instance.
    /// </summary>
    public sealed class CircuitCurrentTenant : ICurrentTenant, IDisposable
    {
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private int? _organizationId;

        public CircuitCurrentTenant(AuthenticationStateProvider authenticationStateProvider)
        {
            _authenticationStateProvider = authenticationStateProvider;
            _authenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
        }

        public int? OrganizationId => _organizationId;

        public async Task PrimeAsync()
        {
            var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
            SetFromState(state);
        }

        private void OnAuthenticationStateChanged(Task<AuthenticationState> task)
        {
            _ = ApplyAsync(task);
        }

        private async Task ApplyAsync(Task<AuthenticationState> task)
        {
            var state = await task;
            SetFromState(state);
        }

        private void SetFromState(AuthenticationState state)
        {
            var claim = state.User.FindFirst(CheckbusClaims.OrganizationId);
            _organizationId = claim is not null && int.TryParse(claim.Value, out var organizationId)
                ? organizationId
                : null;
        }

        public void Dispose()
        {
            _authenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        }
    }
}
