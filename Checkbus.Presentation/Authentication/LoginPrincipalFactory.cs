using System.Globalization;
using System.Security.Claims;
using Checkbus.Application.UseCases.Authentication.Login;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Checkbus.Presentation.Authentication
{
    /// <summary>
    /// Maps a successful <see cref="LoginResult"/> to the <see cref="ClaimsPrincipal"/> signed in via
    /// <c>SignInAsync</c> (see design D3 and the authentication-session spec).
    /// </summary>
    public static class LoginPrincipalFactory
    {
        public static ClaimsPrincipal Create(LoginResult result)
        {
            if (!result.Success)
            {
                throw new InvalidOperationException("Cannot create a principal from a failed login result.");
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, result.UserId.ToString(CultureInfo.InvariantCulture)),
                new(ClaimTypes.Email, result.Email ?? string.Empty),
                new(ClaimTypes.Name, result.Email ?? string.Empty),
                new(CheckbusClaims.OrganizationId, result.OrganizationId.ToString(CultureInfo.InvariantCulture)),
                new(CheckbusClaims.OrganizationName, result.OrganizationName ?? string.Empty),
                new(CheckbusClaims.MustChangePassword, result.MustChangePassword.ToString(CultureInfo.InvariantCulture))
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identity);
        }
    }
}
