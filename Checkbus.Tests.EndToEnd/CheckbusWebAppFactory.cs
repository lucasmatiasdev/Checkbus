using Checkbus.DAL.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Checkbus.Tests.EndToEnd;

/// <summary>
/// Hosts the REAL Checkbus.UI application (real Program.cs, real endpoints,
/// real Blazor Server interactive circuits) on a real Kestrel listener bound
/// to a dynamic loopback port, so a genuine browser (Playwright) can drive it
/// — unlike the in-memory <see cref="WebApplicationFactory{TEntryPoint}"/>
/// TestServer, a real circuit needs an actual HTTP+WebSocket endpoint to
/// negotiate SignalR against.
///
/// The only override is persistence: the real Npgsql-backed
/// <c>DbContextOptions&lt;CheckbusDbContext&gt;</c> singleton (which needs a
/// real local Postgres password this test run does not have) is swapped for
/// a SQLite in-memory database on a single connection kept open for the
/// factory's lifetime. Every other service — <c>ITenantProvider</c>,
/// <c>CircuitTenantState</c>, <c>TenantScopedDbContextFactory</c>,
/// <c>IFleetService</c>, real cookie authentication, real antiforgery — stays
/// exactly as production wires it, because those are the pieces this test
/// exists to exercise for real.
/// </summary>
public class CheckbusWebAppFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private IHost? _kestrelHost;

    public string ServerAddress { get; private set; } = string.Empty;

    public DbContextOptions<CheckbusDbContext> DbContextOptions { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        DbContextOptions = new DbContextOptionsBuilder<CheckbusDbContext>()
            .UseSqlite(_connection)
            .Options;

        // A dynamic loopback port ("port 0") avoids collisions when the full
        // suite or repeated local runs launch more than one real Kestrel
        // instance.
        builder.UseUrls("http://127.0.0.1:0");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<CheckbusDbContext>>();
            services.AddSingleton(DbContextOptions);
        });

        base.ConfigureWebHost(builder);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // WebApplicationFactory always wires an in-memory TestServer
        // internally, which is fine for HttpClient-based tests but useless
        // for a real browser — Playwright/Chromium needs an actual TCP
        // listener + real WebSocket upgrade for the SignalR circuit. The
        // documented workaround (see the ASP.NET Core Blazor Playwright
        // testing samples) is to build BOTH hosts: the TestServer host that
        // satisfies WebApplicationFactory's internal contract (started but
        // never used), and a second, real Kestrel host built from the same
        // configured builder that Playwright actually talks to.
        var testHost = builder.Build();

        builder.ConfigureWebHost(webHostBuilder => webHostBuilder.UseKestrel());
        _kestrelHost = builder.Build();
        _kestrelHost.Start();

        var server = _kestrelHost.Services.GetRequiredService<IServer>();
        var addressesFeature = server.Features.Get<IServerAddressesFeature>()
            ?? throw new InvalidOperationException("Kestrel did not report a bound server address.");
        ServerAddress = addressesFeature.Addresses.Select(a => new Uri(a)).Last().ToString().TrimEnd('/');

        using (var context = new CheckbusDbContext(DbContextOptions))
        {
            context.Database.EnsureCreated();
        }

        testHost.Start();
        return testHost;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _kestrelHost?.Dispose();
            _connection.Dispose();
        }
    }
}
