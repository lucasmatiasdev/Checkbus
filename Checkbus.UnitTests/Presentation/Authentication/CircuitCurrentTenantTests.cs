using System.Security.Claims;
using Checkbus.Presentation.Authentication;
using Microsoft.AspNetCore.Components.Authorization;

namespace Checkbus.UnitTests.Presentation.Authentication
{
    public class CircuitCurrentTenantTests
    {
        private sealed class FakeAuthenticationStateProvider : AuthenticationStateProvider
        {
            private AuthenticationState _current;
            private int _resolveCallCount;

            public FakeAuthenticationStateProvider(ClaimsPrincipal principal)
            {
                _current = new AuthenticationState(principal);
            }

            public int ResolveCallCount => _resolveCallCount;

            public override Task<AuthenticationState> GetAuthenticationStateAsync()
            {
                _resolveCallCount++;
                return Task.FromResult(_current);
            }

            public void SetPrincipal(ClaimsPrincipal principal)
            {
                _current = new AuthenticationState(principal);
                NotifyAuthenticationStateChanged(Task.FromResult(_current));
            }
        }

        private static ClaimsPrincipal AuthenticatedPrincipal(int organizationId, string? organizationName = null)
        {
            var claims = new List<Claim> { new(CheckbusClaims.OrganizationId, organizationId.ToString()) };
            if (organizationName is not null)
            {
                claims.Add(new Claim(CheckbusClaims.OrganizationName, organizationName));
            }

            var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        private static ClaimsPrincipal AnonymousPrincipal() => new(new ClaimsIdentity());

        [Fact]
        public async Task PrimeAsync_AuthenticatedPrincipal_CachesOrganizationId()
        {
            var provider = new FakeAuthenticationStateProvider(AuthenticatedPrincipal(9));
            var sut = new CircuitCurrentTenant(provider);

            await sut.PrimeAsync();

            Assert.Equal(9, sut.OrganizationId);
            Assert.Equal(9, sut.OrganizationId);
            Assert.Equal(1, provider.ResolveCallCount);
        }

        [Fact]
        public async Task PrimeAsync_AuthenticatedPrincipal_CachesOrganizationName()
        {
            var provider = new FakeAuthenticationStateProvider(AuthenticatedPrincipal(9, "Checkbus Norte"));
            var sut = new CircuitCurrentTenant(provider);

            await sut.PrimeAsync();

            Assert.Equal("Checkbus Norte", sut.OrganizationName);
        }

        [Fact]
        public async Task PrimeAsync_AnonymousPrincipal_ReturnsNullOrganizationId()
        {
            var provider = new FakeAuthenticationStateProvider(AnonymousPrincipal());
            var sut = new CircuitCurrentTenant(provider);

            await sut.PrimeAsync();

            Assert.Null(sut.OrganizationId);
        }

        [Fact]
        public async Task PrimeAsync_AnonymousPrincipal_ReturnsNullOrganizationName()
        {
            var provider = new FakeAuthenticationStateProvider(AnonymousPrincipal());
            var sut = new CircuitCurrentTenant(provider);

            await sut.PrimeAsync();

            Assert.Null(sut.OrganizationName);
        }
    }
}
