using Checkbus.Infrastructure.Seeding;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Checkbus.UnitTests.Presentation
{
    /// <summary>
    /// Covers the Development-only gate at the <c>Program.cs</c> call site (spec "No Migration
    /// Outside Development", "Non-Development environment") — a Testing Obligation requirement the
    /// design's test list omitted. Real ASP.NET Core host via <see cref="WebApplicationFactory{TEntryPoint}"/>;
    /// requires the <c>Checkbus.Presentation</c> <c>InternalsVisibleTo</c> entry added in task 1.1.
    ///
    /// Deviation from the tasks artifact's literal wording (noted in apply-progress): task 5.2
    /// originally called for asserting "&gt;= 1 Organization row after startup" against the shared
    /// local Development Postgres. That database already carries pre-existing Organization rows
    /// from unrelated manual work, so the guard always skips there and the assertion would pass
    /// trivially regardless of whether the gate wired the seeder correctly. Substituting
    /// <see cref="ISeedDataStore"/> proves the actual DI interaction (the guard method is called)
    /// without depending on — or mutating — that shared database's row count.
    /// </summary>
    public class ProgramDevelopmentGateIntegrationTests
    {
        [Fact]
        public void Startup_ProductionWithUnreachableConnectionString_DoesNotThrow()
        {
            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Production");
                    // RFC 5737 TEST-NET-1: reserved, guaranteed unreachable. If Migrate/Seed ran
                    // here, connecting to it would throw.
                    builder.UseSetting(
                        "ConnectionStrings:CheckbusDb",
                        "Host=192.0.2.1;Port=5432;Database=unreachable;Username=none;Password=none;Timeout=1;Command Timeout=1");
                });

            // Accessing Services builds and starts the real host, running every statement between
            // builder.Build() and app.Run() in Program.cs — including the Development gate. If
            // Migrate/Seed ran here, connecting to the unreachable host would throw.
            var exception = Record.Exception(() => _ = factory.Services);

            Assert.Null(exception);
        }

        [Fact]
        public void Startup_Development_InvokesSeederIdempotencyGuard()
        {
            var seedStore = Substitute.For<ISeedDataStore>();
            // Force the skip branch so this test never persists real rows into the shared local
            // Development database — it only proves DevelopmentDataSeeder.SeedAsync() runs.
            seedStore.HasAnyOrganizationAsync(Arg.Any<CancellationToken>()).Returns(true);

            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Development");
                    builder.ConfigureServices(services =>
                    {
                        services.AddScoped<ISeedDataStore>(_ => seedStore);
                    });
                });

            // Development picks up the real local connection string via UserSecrets (loaded
            // automatically for this environment), so Database.MigrateAsync() runs against the
            // real, already-migrated dev schema — a safe no-op.
            var exception = Record.Exception(() => _ = factory.Services);

            Assert.Null(exception);
            seedStore.Received(1).HasAnyOrganizationAsync(Arg.Any<CancellationToken>());
        }
    }
}
