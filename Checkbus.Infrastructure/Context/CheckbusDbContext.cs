using Checkbus.Domain.Entities.Authentication;
using Checkbus.Domain.Entities.Authentication.Authorization;
using Checkbus.Domain.Entities.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Infrastructure.Context
{
    public class CheckbusDbContext : DbContext
    {
        public CheckbusDbContext(DbContextOptions<CheckbusDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Organization> Organizations => Set<Organization>();
        public DbSet<Profile> Profiles => Set<Profile>();
        public DbSet<Role> Roles => Set<Role>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(CheckbusDbContext).Assembly);

            // D5 (deferred, needs ICurrentTenant): foreach entityType in model
            //   ITenantEntity          -> EF.Property<int>(e,"OrganizationId")  == tenant.Id
            //   IOptionalTenantEntity  -> EF.Property<int?>(e,"OrganizationId") == tenant.Id || == null

            base.OnModelCreating(modelBuilder);
        }
    }
}
