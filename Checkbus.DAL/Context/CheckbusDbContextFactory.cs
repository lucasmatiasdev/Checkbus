using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Checkbus.DAL.Context;

/// <summary>
/// Design-time factory used by the EF Core CLI (`dotnet ef migrations ...`)
/// to construct <see cref="CheckbusDbContext"/> without a running host. The
/// connection string here is only used to generate/apply migrations and is
/// never used at runtime (the composition root supplies the real one). Reads
/// from CHECKBUS_DESIGN_TIME_CONNECTION so no real credential is ever hardcoded
/// in source, falling back to an obviously-fake local placeholder.
/// </summary>
public class CheckbusDbContextFactory : IDesignTimeDbContextFactory<CheckbusDbContext>
{
    public CheckbusDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("CHECKBUS_DESIGN_TIME_CONNECTION")
            ?? "Host=localhost;Database=checkbus;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<CheckbusDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new CheckbusDbContext(optionsBuilder.Options);
    }
}
