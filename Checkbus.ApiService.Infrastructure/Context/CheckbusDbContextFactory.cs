using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Checkbus.ApiService.Infrastructure.Persistence
{

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
