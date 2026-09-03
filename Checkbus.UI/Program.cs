using Checkbus.BLL.DependencyInjection;
using Checkbus.UI.Components;
using Checkbus.UI.Endpoints;
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

// Environment-configurable base path for IFileStorageService (decision #41):
// a developer's own local disk by default, the VPS's local disk once
// deployed via the FileStorage:BasePath configuration value — no code change
// needed to move between environments.
var configuredFileStorageBasePath = builder.Configuration["FileStorage:BasePath"];
var fileStorageBasePath = string.IsNullOrWhiteSpace(configuredFileStorageBasePath)
    ? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "file-storage")
    : configuredFileStorageBasePath;

builder.Services.AddCheckbusApplication(connectionString, fileStorageBasePath);

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
builder.Services.AddAntiforgery();

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

app.MapAccountEndpoints();

app.Run();

/// <summary>
/// Standard .NET 8 marker for top-level-statement <c>Program.cs</c>: makes the
/// otherwise-internal, compiler-generated <c>Program</c> class visible outside
/// this assembly so <c>WebApplicationFactory&lt;Program&gt;</c> can host this
/// exact application in-process for integration/end-to-end tests
/// (Checkbus.Tests.EndToEnd).
/// </summary>
public partial class Program;
