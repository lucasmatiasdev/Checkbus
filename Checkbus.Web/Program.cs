using Checkbus.Web;
using Checkbus.Web.Components;
using Checkbus.Web.Extensions;
using Checkbus.Web.Services;
using MudBlazor.Services;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.AddOutputCache();

builder.Services.AddTransient<AuthenticationStateHandler>();

builder.Services.AddHttpClient("apiservice", client =>
{
    client.BaseAddress = new Uri("https+http://apiservice/api/");
})
    .AddApplicationScopeHandler()
    .AddHttpMessageHandler<AuthenticationStateHandler>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "auth_token";
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(4);
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapPost("/account/login", async (HttpContext httpContext, IHttpClientFactory httpClientFactory) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    var email = form["Email"].ToString();
    var password = form["Password"].ToString();

    var client = httpClientFactory.CreateClient("apiservice");
    var response = await client.PostAsJsonAsync("auth/login", new { Email = email, Password = password });

    if (!response.IsSuccessStatusCode)
        return Results.Redirect("/login?error=1");

    var result = await response.Content.ReadFromJsonAsync<LoginApiResult>();

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, result!.UserId.ToString()),
        new(ClaimTypes.Name, result.Email),
        new(ClaimTypes.Role, result.Role),
        new("OrganizationId", result.OrganizationId.ToString()),
        new("access_token", result.Token)
    };
    var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

    await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
    {
        IsPersistent = true,
        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(4)
    });

    return Results.Redirect("/dashboard");
});

app.MapPost("/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();

record LoginApiResult(string Token, Guid UserId, Guid OrganizationId, string Role, string Email);
