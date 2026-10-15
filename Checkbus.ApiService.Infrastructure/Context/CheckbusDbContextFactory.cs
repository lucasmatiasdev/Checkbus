using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Checkbus.ApiService.Infrastructure.Persistence
{
    // Design-time-only factory. `dotnet ef` (migrations add/remove, database update, etc.) cannot
    // resolve CheckbusDbContext through the real composition root in Program.cs, because that
    // wiring goes through Aspire's AddNpgsqlDbContext — which depends on Aspire's own orchestration
    // (service discovery, the AppHost-injected "checkbusdb" connection string) that simply does not
    // run under the EF CLI tooling process. This factory exists only to give `dotnet ef` something
    // to construct the DbContext with; it is never referenced by the running application.
    //
    // The connection string below is a placeholder pointing at a conventional local Postgres
    // instance. It only matters for schema-diffing purposes (`dotnet ef migrations add`), which
    // needs no live connection at all — EF reads the current C# model, not an actual database, to
    // produce a migration. It would only need to resolve to a real, reachable database for
    // `dotnet ef database update`, which is not how this project applies migrations (see
    // Program.cs's MigrateAsync() call and the baseline migration's own stamping-procedure comment
    // for how a migration actually gets applied in Development/production).
    //
    // IMPORTANT for anyone adding a schema change: any future entity/property/relationship change
    // to CheckbusDbContext's model needs its own `dotnet ef migrations add <Name> --project
    // Checkbus.ApiService.Infrastructure --startup-project Checkbus.ApiService` — EF Core does not
    // infer schema changes at runtime, only from migrations that exist as files.
    public class CheckbusDbContextFactory : IDesignTimeDbContextFactory<CheckbusDbContext>
    {
        public CheckbusDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<CheckbusDbContext>();
            optionsBuilder.UseNpgsql(
                "Host=localhost;Port=5432;Database=checkbus_design;Username=postgres;Password=postgres");

            return new CheckbusDbContext(optionsBuilder.Options);
        }
    }
}
