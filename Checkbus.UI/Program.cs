using System.Security.Claims;
using Checkbus.BEL.Auth;
using Checkbus.BLL.Auth;
using Checkbus.BLL.DependencyInjection;
using Checkbus.BLL.Organization;
using Checkbus.UI.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddMudServices();

var connectionString = builder.Configuration.GetConnectionString("CheckbusDb")
    ?? throw new InvalidOperationException("Connection string 'CheckbusDb' is not configured.");

builder.Services.AddCheckbusApplication(connectionString);

// Minimal custom cookie authentication against Checkbus.BEL.Auth.User (CU-01):
// deliberately not ASP.NET Core Identity's own user store, so CheckbusDbContext
// stays the single database per the design's single-database decision.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Plain (non-Blazor-circuit) sign-in/sign-out endpoints. Login and company
// registration submit here via ordinary HTML form posts so that
// HttpContext.SignInAsync always runs in a normal request/response, never
// from inside an already-upgraded interactive Blazor Server circuit.
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
    return Results.Redirect("/");
});

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
        return Results.Redirect("/");
    }
    catch (InvalidOperationException)
    {
        return Results.Redirect("/empresas/nueva?error=1");
    }
});

app.MapPost("/account/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.Run();

static async Task SignInAsync(
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
