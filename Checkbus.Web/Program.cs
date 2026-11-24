using Checkbus.Web;
using Checkbus.Web.Components;
using Checkbus.Web.Extensions;
using Checkbus.Web.Features.Audit.Services;
using Checkbus.Web.Features.Boarding.Services;
using Checkbus.Web.Features.DriverRequirements.Services;
using Checkbus.Web.Features.Events.Services;
using Checkbus.Web.Features.MaintenanceRecords.Services;
using Checkbus.Web.Features.Notifications.Services;
using Checkbus.Web.Features.Organizations.Services;
using Checkbus.Web.Features.Payments.Services;
using Checkbus.Web.Features.PublicReservations.Services;
using Checkbus.Web.Features.SupportTickets.Services;
using Checkbus.Web.Features.Trips.Services;
using Checkbus.Web.Features.TripsReport.Services;
using Checkbus.Web.Features.Users.Services;
using Checkbus.Web.Features.VehicleDiagnostics.Services;
using Checkbus.Web.Features.VehicleDocuments.Services;
using Checkbus.Web.Features.Vehicles.Services;
using Checkbus.Web.Shared.Models;
using Checkbus.Web.Shared.Services;
using MudBlazor.Services;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Globalization;

var argentineCulture = new CultureInfo("es-AR");
CultureInfo.DefaultThreadCurrentCulture = argentineCulture;
CultureInfo.DefaultThreadCurrentUICulture = argentineCulture;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.AddOutputCache();

var googleMapsBrowserOptions = builder.Configuration.GetSection("GoogleMaps").Get<GoogleMapsBrowserOptions>()
    ?? new GoogleMapsBrowserOptions();

builder.Services.AddSingleton(googleMapsBrowserOptions);

builder.Services.AddTransient<AuthenticationStateHandler>();

builder.Services.AddHttpClient("apiservice", client =>
{

    client.BaseAddress = new Uri("http://apiservice/api/");
})
    .AddApplicationScopeHandler()
    .AddHttpMessageHandler<AuthenticationStateHandler>();

builder.Services.AddScoped<UserRegistrationClient>();
builder.Services.AddScoped<OrganizationRegistrationClient>();
builder.Services.AddScoped<UsersClient>();
builder.Services.AddScoped<DriverRequirementsClient>();
builder.Services.AddScoped<VehiclesClient>();
builder.Services.AddScoped<VehicleDocumentsClient>();
builder.Services.AddScoped<SupportTicketsClient>();
builder.Services.AddScoped<SupportTicketsAdminClient>();
builder.Services.AddScoped<AuditLogClient>();
builder.Services.AddScoped<OrganizationsClient>();
builder.Services.AddScoped<UsersByOrganizationClient>();
builder.Services.AddScoped<MaintenanceRecordsClient>();
builder.Services.AddScoped<VehicleDiagnosticsClient>();
builder.Services.AddScoped<EventsClient>();
builder.Services.AddScoped<TripsClient>();
builder.Services.AddScoped<TripsReportClient>();
builder.Services.AddScoped<PublicTripsClient>();
builder.Services.AddScoped<PublicOrganizationsClient>();
builder.Services.AddScoped<PublicReservationsClient>();
builder.Services.AddScoped<BoardingClient>();
builder.Services.AddScoped<PaymentsClient>();
builder.Services.AddScoped<NotificationsClient>();

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

app.MapGet("/trips/report/export", async (
    HttpContext httpContext,
    ClaimsPrincipal user,
    IHttpClientFactory httpClientFactory,
    CancellationToken cancellationToken) =>
{
    var token = user.FindFirst("access_token")?.Value;
    if (string.IsNullOrEmpty(token))
        return Results.Unauthorized();

    var httpClient = httpClientFactory.CreateClient("apiservice");
    using var request = new HttpRequestMessage(HttpMethod.Get, $"Trips/report/export{httpContext.Request.QueryString}");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    if (!response.IsSuccessStatusCode)
        return Results.StatusCode((int)response.StatusCode);

    var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
    var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
    var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
        ?? response.Content.Headers.ContentDisposition?.FileName
        ?? "reporte-viajes";
    return Results.Stream(stream, contentType, fileDownloadName: fileName);
})
.RequireAuthorization();

app.MapGet("/support-tickets/{id:guid}/attachment", async (
    Guid id,
    ClaimsPrincipal user,
    IHttpClientFactory httpClientFactory,
    CancellationToken cancellationToken) =>
{
    var token = user.FindFirst("access_token")?.Value;
    if (string.IsNullOrEmpty(token))
        return Results.Unauthorized();

    var httpClient = httpClientFactory.CreateClient("apiservice");
    using var request = new HttpRequestMessage(HttpMethod.Get, $"SupportTickets/{id}/attachment");
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
