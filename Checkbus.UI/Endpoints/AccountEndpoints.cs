using System.Security.Claims;
using Checkbus.BEL.Auth;
using Checkbus.BLL.Auth;
using Checkbus.BLL.Organization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Checkbus.UI.Endpoints;

/// <summary>
/// Plain (non-Blazor-circuit) sign-in/sign-out endpoints. Login and company
/// registration submit here via ordinary HTML form posts so that
/// HttpContext.SignInAsync always runs in a normal request/response, never
/// from inside an already-upgraded interactive Blazor Server circuit.
/// </summary>
internal static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/account/login", async (HttpContext http, IAuthService authService) =>
        {
            var form = await http.Request.ReadFormAsync();
            var email = form["email"].ToString();
            var password = form["password"].ToString();

            var authenticated = await authService.AuthenticateAsync(email, password);
            if (authenticated is null)
            {
                return Results.Redirect("/login?error=1");
            }

            await SignInAsync(http, authenticated.UserId, authenticated.Email, authenticated.FullName,
                authenticated.OrganizationId, authenticated.RoleId, authenticated.RoleName);
            return Results.Redirect("/app");
        }).ValidateAntiforgery();

        app.MapPost("/account/register-company", async (HttpContext http, ICompanyRegistrationService registrationService) =>
        {
            var form = await http.Request.ReadFormAsync();
            var organizationName = form["organizationName"].ToString();
            var cuit = form["cuit"].ToString();
            var adminFullName = form["adminFullName"].ToString();
            var adminEmail = form["adminEmail"].ToString();
            var adminPassword = form["adminPassword"].ToString();

            try
            {
                var registered = await registrationService.RegisterCompanyAsync(
                    organizationName, cuit, adminFullName, adminEmail, adminPassword);

                await SignInAsync(http, registered.AdminUserId, adminEmail, adminFullName,
                    registered.OrganizationId, registered.AdminRoleId, registered.AdminRoleName);
                return Results.Redirect("/app");
            }
            catch (InvalidOperationException)
            {
                return Results.Redirect("/company-registration?error=1");
            }
        }).ValidateAntiforgery();

        app.MapPost("/account/logout", async (HttpContext http) =>
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/");
        }).ValidateAntiforgery();
    }

    private static async Task SignInAsync(
        HttpContext http, int userId, string email, string fullName, int organizationId, int roleId, string roleName)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, fullName),
            new(ClaimTypes.Role, roleName),
            new(CheckbusClaimTypes.OrganizationId, organizationId.ToString()),
            new(CheckbusClaimTypes.RoleId, roleId.ToString()),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }
}
