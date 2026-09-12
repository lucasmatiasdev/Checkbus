using Microsoft.Extensions.Configuration;

namespace Checkbus.UnitTests.TestDoubles
{
    /// <summary>
    /// Resolves the local Postgres connection string used by real-database integration tests
    /// (D9). Reads the same user-secrets store as Checkbus.Presentation (shared UserSecretsId) so
    /// the real password never lands in source control, with an environment variable override
    /// (`ConnectionStrings__CheckbusDb`) for CI.
    /// </summary>
    public static class PostgresTestConnection
    {
        public static string ConnectionString
        {
            get
            {
                var configuration = new ConfigurationBuilder()
                    .AddUserSecrets(typeof(PostgresTestConnection).Assembly, optional: true)
                    .AddEnvironmentVariables()
                    .Build();

                return configuration.GetConnectionString("CheckbusDb")
                    ?? throw new InvalidOperationException(
                        "Connection string 'CheckbusDb' not configured for integration tests. Set it via " +
                        "'dotnet user-secrets set ConnectionStrings:CheckbusDb ... --project Checkbus.Presentation' " +
                        "(shared UserSecretsId) or the ConnectionStrings__CheckbusDb environment variable.");
            }
        }
    }
}
