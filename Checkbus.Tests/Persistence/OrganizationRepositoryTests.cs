using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Persistence;

public class OrganizationRepositoryTests
{
    private static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=True");
        connection.Open();
        return connection;
    }

    private static CheckbusDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<CheckbusDbContext>()
            .UseSqlite(connection)
            .Options;
        return new CheckbusDbContext(options);
    }

    private static Organization CreateOrganization(string slug) => new()
    {
        Id = Guid.NewGuid(),
        CUIT = "20-12345678-9",
        Name = "Checkbus Demo",
        Slug = slug,
        LogoUrl = "",
        IsActive = true
    };

    [Fact]
    public async Task GetSlugByIdAsync_OrganizationExists_ReturnsSlug()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        db.Organizations.Add(organization);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new OrganizationRepository(db);

        var slug = await repository.GetSlugByIdAsync(organization.Id, cancellationToken);

        Assert.Equal("checkbus-demo", slug);
    }

    [Fact]
    public async Task GetSlugByIdAsync_OrganizationMissing_ReturnsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new OrganizationRepository(db);

        var slug = await repository.GetSlugByIdAsync(Guid.NewGuid(), cancellationToken);

        Assert.Null(slug);
    }

    [Fact]
    public async Task Slug_DuplicateInsert_IsRejectedByUniqueIndex()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        db.Organizations.Add(CreateOrganization("checkbus-demo"));
        await db.SaveChangesAsync(cancellationToken);

        db.Organizations.Add(CreateOrganization("checkbus-demo"));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(cancellationToken));
    }
}
