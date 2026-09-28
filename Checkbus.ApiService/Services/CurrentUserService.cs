using Checkbus.ApiService.Application.Interfaces.Authentication;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Checkbus.ApiService.Services
{
    /// <summary>
    /// Reads the authenticated caller's identity exclusively from the validated
    /// <see cref="ClaimsPrincipal"/> attached to the current request. Never derives
    /// identity from the request body, query string, or route values (anti-IDOR).
    /// Registered Scoped: <see cref="IHttpContextAccessor"/> exposes per-request state,
    /// so a Singleton registration would cache one request's identity and leak it into
    /// every later request served by the same instance.
    /// </summary>
    public sealed class CurrentUserService : ICurrentUserService
    {
        internal const string OrganizationIdClaimType = "OrganizationId";

        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

        public Guid? UserId => ParseGuid(ClaimIfAuthenticated(ClaimTypes.NameIdentifier));

        public Guid? OrganizationId => ParseGuid(ClaimIfAuthenticated(OrganizationIdClaimType));

        public string? Role => ClaimIfAuthenticated(ClaimTypes.Role);

        public string? Username => ClaimIfAuthenticated(ClaimTypes.Name);

        private string? ClaimIfAuthenticated(string claimType) =>
            IsAuthenticated ? Principal?.FindFirst(claimType)?.Value : null;

        private static Guid? ParseGuid(string? value) =>
            Guid.TryParse(value, out var guid) ? guid : null;
    }
}
