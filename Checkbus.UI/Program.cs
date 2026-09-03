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
