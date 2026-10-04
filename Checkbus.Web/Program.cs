using Checkbus.Web;
using Checkbus.Web.Components;
using Checkbus.Web.Extensions;
using Checkbus.Web.Models;
using Checkbus.Web.Services;
using MudBlazor.Services;
using System.Net.Http.Headers;
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

// Browser-exposed Google Maps key for LocationPicker's client-side Maps JavaScript API load —
// a different key from Checkbus.ApiService's server-side Directions key (see
// GoogleMapsBrowserOptions's doc comment). Binds permissively like its ApiService counterpart:
// no key has been provisioned yet, so a missing "GoogleMaps" section must not fail startup.
var googleMapsBrowserOptions = builder.Configuration.GetSection("GoogleMaps").Get<GoogleMapsBrowserOptions>()
    ?? new GoogleMapsBrowserOptions();

builder.Services.AddSingleton(googleMapsBrowserOptions);

builder.Services.AddTransient<AuthenticationStateHandler>();

builder.Services.AddHttpClient("apiservice", client =>
{
    client.BaseAddress = new Uri("https+http://apiservice/api/");
})
    .AddApplicationScopeHandler()
    .AddHttpMessageHandler<AuthenticationStateHandler>();

builder.Services.AddScoped<UserRegistrationClient>();
builder.Services.AddScoped<UsersClient>();
builder.Services.AddScoped<DriverRequirementsClient>();
builder.Services.AddScoped<VehiclesClient>();
builder.Services.AddScoped<VehicleDocumentsClient>();
builder.Services.AddScoped<MaintenanceRecordsClient>();
builder.Services.AddScoped<VehicleDiagnosticsClient>();
builder.Services.AddScoped<EventosClient>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "auth_token";
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
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

// The browser can't directly hit the API's file-download endpoint — the bearer token normally
// gets attached via AuthenticationStateHandler on the keyed "apiservice" HttpClient, but that
// mechanism reads AuthenticationStateProvider.GetAuthenticationStateAsync(), which
// ServerAuthenticationStateProvider explicitly refuses to run outside a live Razor component's
// circuit DI scope (throws InvalidOperationException — confirmed via manual smoke test, this is
// not a theoretical concern). A minimal API endpoint's HTTP request scope is not that circuit, so
// this handler resolves a plain (non-keyed) "apiservice" HttpClient via IHttpClientFactory — which
// skips ApplicationScopeHandler entirely, so AuthenticationStateHandler's scope-option check never
// fires — and attaches the bearer token itself by reading the same "access_token" claim directly
// off the already-authenticated HttpContext.User, which IS valid in this request scope.
app.MapGet("/driver-documents/{userId:guid}/{type}", async (
    Guid userId,
    string type,
    ClaimsPrincipal user,
    IHttpClientFactory httpClientFactory,
    CancellationToken cancellationToken) =>
{
    var token = user.FindFirst("access_token")?.Value;
    if (string.IsNullOrEmpty(token))
        return Results.Unauthorized();

    var httpClient = httpClientFactory.CreateClient("apiservice");
    using var request = new HttpRequestMessage(HttpMethod.Get, $"DriverRequirements/{userId}/{type}/document");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    if (!response.IsSuccessStatusCode)
        return Results.StatusCode((int)response.StatusCode);

    var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
    var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
    return Results.Stream(stream, contentType);
})
.RequireAuthorization();

// Same corrected pattern as the driver-documents proxy above (see its comment for the full
// rationale): a plain (non-keyed) "apiservice" HttpClient via IHttpClientFactory, with the bearer
// token attached manually from the already-authenticated HttpContext.User's "access_token" claim —
// never the keyed client, which depends on a live Razor circuit this minimal-API endpoint does not
// have.
app.MapGet("/vehicle-documents/{vehicleId:guid}/{type}", async (
    Guid vehicleId,
    string type,
    ClaimsPrincipal user,
    IHttpClientFactory httpClientFactory,
    CancellationToken cancellationToken) =>
{
    var token = user.FindFirst("access_token")?.Value;
    if (string.IsNullOrEmpty(token))
        return Results.Unauthorized();

    var httpClient = httpClientFactory.CreateClient("apiservice");
    using var request = new HttpRequestMessage(HttpMethod.Get, $"VehicleDocuments/{vehicleId}/{type}/document");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    if (!response.IsSuccessStatusCode)
        return Results.StatusCode((int)response.StatusCode);

    var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
    var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
    return Results.Stream(stream, contentType);
})
.RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();

record LoginApiResult(string Token, Guid UserId, Guid OrganizationId, string Role, string Email);
