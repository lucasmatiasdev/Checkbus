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
    public async Task GetNameByIdAsync_OrganizationExists_ReturnsName()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("brahman");
        db.Organizations.Add(organization);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new OrganizationRepository(db);

        var name = await repository.GetNameByIdAsync(organization.Id, cancellationToken);

        Assert.Equal("Checkbus Demo", name);
    }

    [Fact]
    public async Task GetNameByIdAsync_OrganizationMissing_ReturnsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new OrganizationRepository(db);

        var name = await repository.GetNameByIdAsync(Guid.NewGuid(), cancellationToken);

        Assert.Null(name);
    }

    [Fact]
    public async Task GetIdBySlugAsync_OrganizationExists_ReturnsId()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        db.Organizations.Add(organization);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new OrganizationRepository(db);

        var id = await repository.GetIdBySlugAsync("checkbus-demo", cancellationToken);

        Assert.Equal(organization.Id, id);
    }

    [Fact]
    public async Task GetIdBySlugAsync_SlugDoesNotExist_ReturnsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new OrganizationRepository(db);

        var id = await repository.GetIdBySlugAsync("unknown-slug", cancellationToken);

        Assert.Null(id);
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

    [Fact]
    public async Task AddAsync_PersistsOrganizationAndIsRetrievableAfterwards()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new OrganizationRepository(db);
        var organization = CreateOrganization("checkbus-demo");

        await repository.AddAsync(organization, cancellationToken);

        var slug = await repository.GetSlugByIdAsync(organization.Id, cancellationToken);
        Assert.Equal("checkbus-demo", slug);
    }

    [Fact]
    public async Task CuitExistsAsync_CuitRegistered_ReturnsTrue()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        db.Organizations.Add(CreateOrganization("checkbus-demo"));
        await db.SaveChangesAsync(cancellationToken);

        var repository = new OrganizationRepository(db);

        Assert.True(await repository.CuitExistsAsync("20-12345678-9", cancellationToken));
    }

    [Fact]
    public async Task CuitExistsAsync_CuitNotRegistered_ReturnsFalse()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new OrganizationRepository(db);

        Assert.False(await repository.CuitExistsAsync("20-12345678-9", cancellationToken));
    }

    [Fact]
    public async Task CuitExistsAsync_IsGlobal_NotScopedToAnySingleOrganization()
    {
        // A CUIT belongs to exactly one Organization system-wide, unlike Trip.Slug which is
        // only unique per-organization — this proves the lookup takes no organization scope at
        // all and still finds a CUIT registered under a different, unrelated organization.
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var other = CreateOrganization("another-company");
        other.CUIT = "30-98765432-1";
        db.Organizations.Add(other);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new OrganizationRepository(db);

        Assert.True(await repository.CuitExistsAsync("30-98765432-1", cancellationToken));
    }

    [Fact]
    public async Task SlugExistsAsync_SlugRegistered_ReturnsTrue()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        db.Organizations.Add(CreateOrganization("checkbus-demo"));
        await db.SaveChangesAsync(cancellationToken);

        var repository = new OrganizationRepository(db);

        Assert.True(await repository.SlugExistsAsync("checkbus-demo", cancellationToken));
    }

    [Fact]
    public async Task SlugExistsAsync_SlugNotRegistered_ReturnsFalse()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new OrganizationRepository(db);

        Assert.False(await repository.SlugExistsAsync("checkbus-demo", cancellationToken));
    }
}
