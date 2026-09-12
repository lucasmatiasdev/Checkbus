using Checkbus.Application.Abstractions;
using Checkbus.Domain.Entities.Authentication;
using Checkbus.Domain.Entities.Authentication.Authorization;
using Checkbus.Domain.Entities.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Infrastructure.Context
{
    public class CheckbusDbContext : DbContext
    {
        // D4: never a constructor parameter — AddDbContextFactory registers IDbContextFactory<T>
        // as Singleton, so a non-options ctor parameter would defeat EF's options-only activation
        // short-circuit and resolve from the ROOT provider (captive singleton tenant). Bound after
        // construction instead; UnauthenticatedTenant is the fail-closed default (D5) until then.
        private ICurrentTenant _tenant = UnauthenticatedTenant.Instance;

        public CheckbusDbContext(DbContextOptions<CheckbusDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Organization> Organizations => Set<Organization>();
        public DbSet<Profile> Profiles => Set<Profile>();
        public DbSet<Role> Roles => Set<Role>();

        internal void BindTenant(ICurrentTenant tenant) => _tenant = tenant;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(CheckbusDbContext).Assembly);

            // D6: fail-closed global tenant filters. _tenant is an instance field, so EF
            // re-evaluates it against the context instance running each query (proven by the D9
            // integration test), rather than baking one tenant into the cached model.
            modelBuilder.Entity<User>().HasQueryFilter(u =>
                _tenant.OrganizationId != null && u.OrganizationId == _tenant.OrganizationId);

            modelBuilder.Entity<Profile>().HasQueryFilter(p =>
                p.OrganizationId == null ||
                (_tenant.OrganizationId != null && p.OrganizationId == _tenant.OrganizationId));

            base.OnModelCreating(modelBuilder);
        }
    }
}
