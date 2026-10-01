using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Persistence;

public class UserRepositoryTests
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

    private static User CreateUser(Guid organizationId, string documentNumber, string email) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Jose",
        Surname = "Diaz",
        Email = email,
        PasswordHash = "hashed-password",
        DocumentNumber = documentNumber,
        Role = Role.Chofer,
        OrganizationId = organizationId,
        IsActive = true
    };

    [Fact]
    public async Task DocumentNumberExistsInOrganizationAsync_SameOrgSameNumber_ReturnsTrue()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        db.Organizations.Add(organization);
        db.Users.Add(CreateUser(organization.Id, "30111222", "jose.diaz@checkbus-demo.com"));
        await db.SaveChangesAsync(cancellationToken);

        var repository = new UserRepository(db);

        var exists = await repository.DocumentNumberExistsInOrganizationAsync(
            "30111222", organization.Id, cancellationToken);

        Assert.True(exists);
    }

    [Fact]
    public async Task DocumentNumberExistsInOrganizationAsync_DifferentOrgSameNumber_ReturnsFalse()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organizationA = CreateOrganization("org-a");
        var organizationB = CreateOrganization("org-b");
        db.Organizations.AddRange(organizationA, organizationB);
        db.Users.Add(CreateUser(organizationA.Id, "30111222", "jose.diaz@org-a.com"));
        await db.SaveChangesAsync(cancellationToken);

        var repository = new UserRepository(db);

        var exists = await repository.DocumentNumberExistsInOrganizationAsync(
            "30111222", organizationB.Id, cancellationToken);

        Assert.False(exists);
    }

    [Fact]
    public async Task FindEmailsByLocalPartPrefixAsync_ReturnsMatchingRowsOnly()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        db.Organizations.Add(organization);
        db.Users.AddRange(
            CreateUser(organization.Id, "1", "jose.diaz@checkbus-demo.com"),
            CreateUser(organization.Id, "2", "jose.diaz1@checkbus-demo.com"),
            CreateUser(organization.Id, "3", "maria.gomez@checkbus-demo.com"));
        await db.SaveChangesAsync(cancellationToken);

        var repository = new UserRepository(db);

        var emails = await repository.FindEmailsByLocalPartPrefixAsync(
            "jose.diaz", "checkbus-demo.com", cancellationToken);

        Assert.Equal(2, emails.Count);
        Assert.Contains("jose.diaz@checkbus-demo.com", emails);
        Assert.Contains("jose.diaz1@checkbus-demo.com", emails);
        Assert.DoesNotContain("maria.gomez@checkbus-demo.com", emails);
    }

    [Fact]
    public async Task AddAsync_DuplicateDocumentNumberInSameOrganization_IsRejectedByUniqueIndex()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        db.Organizations.Add(organization);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new UserRepository(db);
        await repository.AddAsync(
            CreateUser(organization.Id, "30111222", "jose.diaz@checkbus-demo.com"), cancellationToken);

        await Assert.ThrowsAsync<DbUpdateException>(() => repository.AddAsync(
            CreateUser(organization.Id, "30111222", "jose.diaz1@checkbus-demo.com"), cancellationToken));
    }

    [Fact]
    public async Task AddAsync_SameDocumentNumberInDifferentOrganization_Succeeds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organizationA = CreateOrganization("org-a");
        var organizationB = CreateOrganization("org-b");
        db.Organizations.AddRange(organizationA, organizationB);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new UserRepository(db);
        await repository.AddAsync(CreateUser(organizationA.Id, "30111222", "jose.diaz@org-a.com"), cancellationToken);

        await repository.AddAsync(CreateUser(organizationB.Id, "30111222", "jose.diaz@org-b.com"), cancellationToken);

        Assert.Equal(2, await db.Users.CountAsync(cancellationToken));
    }
}
