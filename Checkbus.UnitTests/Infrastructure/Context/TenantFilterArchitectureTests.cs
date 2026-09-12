using Checkbus.Domain.Interfaces;
using Checkbus.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.UnitTests.Infrastructure.Context
{
    /// <summary>
    /// Guards every current AND future ITenantEntity/IOptionalTenantEntity against being added to
    /// the model without a global query filter (design D6). Model building does not open a real
    /// connection, so this test runs without Postgres.
    /// </summary>
    public class TenantFilterArchitectureTests
    {
        [Fact]
        public void EveryTenantScopedEntity_HasGlobalQueryFilter()
        {
            var options = new DbContextOptionsBuilder<CheckbusDbContext>()
                .UseNpgsql("Host=localhost;Database=architecture-test-only;Username=none;Password=none")
                .Options;

            using var context = new CheckbusDbContext(options);

            var tenantScopedEntityTypes = context.Model.GetEntityTypes()
                .Where(entityType =>
                    typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType) ||
                    typeof(IOptionalTenantEntity).IsAssignableFrom(entityType.ClrType))
                .ToList();

            Assert.NotEmpty(tenantScopedEntityTypes);

            foreach (var entityType in tenantScopedEntityTypes)
            {
                Assert.True(
                    entityType.GetQueryFilter() is not null,
                    $"{entityType.ClrType.Name} implements a tenant interface but has no HasQueryFilter.");
            }
        }
    }
}
