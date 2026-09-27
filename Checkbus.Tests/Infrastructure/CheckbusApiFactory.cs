extern alias ApiServiceAssembly;

using Checkbus.ApiService.Infrastructure.Implementations.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Tests.Infrastructure;

/// <summary>
/// <see cref="WebApplicationFactory{TEntryPoint}"/> for <c>Checkbus.ApiService</c>,
/// running in the "Testing" environment (so Development-only wiring such as
/// <c>MapOpenApi()</c>, health-check endpoints, and <c>EnsureCreatedAsync</c> never runs)
/// with a deterministic, test-controlled <c>Jwt</c> configuration section and a dummy
/// Postgres connection string so <c>AddNpgsqlDbContext</c> registers without connecting.
/// </summary>
/// <remarks>
/// <c>Checkbus.Tests</c> also references <c>Checkbus.AppHost</c>, which has its own
/// top-level-statement <c>Program</c> class, so the entry-point type must be
/// disambiguated via an extern alias on the <c>Checkbus.ApiService</c> reference.
///
/// <para>
/// The configuration overrides below are applied as PROCESS ENVIRONMENT VARIABLES,
/// not via <c>WebHostBuilder.ConfigureAppConfiguration</c>. <c>Checkbus.ApiService/Program.cs</c>
/// reads <c>Jwt:SigningKey</c> (via <c>builder.Configuration.GetSection("Jwt").Get&lt;JwtOptions&gt;()</c>)
/// BEFORE <c>builder.Build()</c> runs. <c>WebApplicationFactory</c>'s config-override hooks for a
/// minimal-hosting (top-level-statement) entry point are only applied at <c>Build()</c> time, which is
/// too late for that eager read — the environment-variable configuration source, by contrast, is already
/// populated by <c>WebApplication.CreateBuilder(args)</c> itself, so it is visible immediately.
/// </para>
/// </remarks>
public sealed class CheckbusApiFactory : WebApplicationFactory<ApiServiceAssembly::Program>
{
    public const string TestSigningKey = "checkbus-test-signing-key-0123456789-abcdefg";
    public const string TestIssuer = "checkbus-api-tests-issuer";
    public const string TestAudience = "checkbus-api-tests-audience";
    public const int TestExpirationMinutes = 240;

    public CheckbusApiFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", TestSigningKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", TestIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", TestAudience);
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", TestExpirationMinutes.ToString());
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__checkbusdb",
            "Host=localhost;Port=5432;Database=checkbus_tests;Username=test;Password=test");
    }

    public static JwtOptions CreateTestJwtOptions() => new()
    {
        SigningKey = TestSigningKey,
        Issuer = TestIssuer,
        Audience = TestAudience,
        ExpirationMinutes = TestExpirationMinutes
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.AddControllers().AddApplicationPart(typeof(RoleProbeController).Assembly);
        });
    }
}
