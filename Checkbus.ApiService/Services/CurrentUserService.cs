using Checkbus.ApiService.Application.Interfaces.Authentication;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Checkbus.ApiService.Services
{
    /// <summary>
    /// Reads the authenticated caller's identity exclusively from the validated
    /// <see cref="ClaimsPrincipal"/> attached to the current request. Never derives
    /// identity from the request body, query string, or route values (anti-IDOR).
    /// Registered Scoped — see design decision 8 for why Singleton must never be used.
    /// </summary>
    public sealed class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

        public Guid? UserId => IsAuthenticated ? ParseGuid(Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value) : null;

        public Guid? OrganizationId => IsAuthenticated ? ParseGuid(Principal?.FindFirst("OrganizationId")?.Value) : null;

        public string? Role => IsAuthenticated ? Principal?.FindFirst(ClaimTypes.Role)?.Value : null;

        public string? Username => IsAuthenticated ? Principal?.FindFirst(ClaimTypes.Name)?.Value : null;

        private static Guid? ParseGuid(string? value) =>
            Guid.TryParse(value, out var guid) ? guid : null;
    }
}
