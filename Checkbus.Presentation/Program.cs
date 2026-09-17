using Checkbus.Application.Abstractions;
using Checkbus.Infrastructure;
using Checkbus.Infrastructure.Context;
using Checkbus.Infrastructure.Seeding;
using Checkbus.Presentation.Authentication;
using Checkbus.Presentation.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// D1: CircuitCurrentTenant is registered as its concrete type (Scoped) so MainLayout can call
// its PrimeAsync() priming method directly, and ICurrentTenant resolves to that same scoped
// instance so CheckbusDbContext's query filter and the concrete type observe one cached value.
// Registered before AddInfrastructure because AddInfrastructure's scoped DbContext delegate
// resolves ICurrentTenant to bind it.
builder.Services.AddScoped<CircuitCurrentTenant>();
builder.Services.AddScoped<ICurrentTenant>(provider => provider.GetRequiredService<CircuitCurrentTenant>());

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "checkbus.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.LoginPath = "/login";
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Development-only: apply pending EF Core migrations and seed Development-only data. Staging and
// Production schema changes stay a deliberate, separate `dotnet ef database update` operation, and
// seeding never runs anywhere else (spec "Automatic Migration at Startup (Development Only)",
// "No Migration Outside Development", "Development-Only Gating").
if (app.Environment.IsDevelopment())
{
    var contextFactory = app.Services.GetRequiredService<IDbContextFactory<CheckbusDbContext>>();
    await using var migrationContext = await contextFactory.CreateDbContextAsync();
    await migrationContext.Database.MigrateAsync();

    using var seedScope = app.Services.CreateScope();
    var seeder = seedScope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>();
    await seeder.SeedAsync();
}

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

app.Run();
