using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Documents;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Persistence;

public class DriverRequirementRepositoryTests
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

    private static DriverRequirement CreateRequirement(
        Guid userId,
        DriverRequirementType type,
        DriverRequirementStatus status = DriverRequirementStatus.Pendiente,
        DateOnly? expirationDate = null) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Type = type,
        Status = status,
        ExpirationDate = expirationDate,
        DocumentPresent = false
    };

    [Fact]
    public async Task AddRangeAsync_PersistsAllRows()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        db.Organizations.Add(organization);
        var user = CreateUser(organization.Id, "30111222", "jose.diaz@checkbus-demo.com");
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new DriverRequirementRepository(db);
        var requirements = new[]
        {
            CreateRequirement(user.Id, DriverRequirementType.LicenciaConducir),
            CreateRequirement(user.Id, DriverRequirementType.CapacitacionProfesional)
        };

        await repository.AddRangeAsync(requirements, cancellationToken);

        Assert.Equal(2, await db.DriverRequirements.CountAsync(cancellationToken));
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsOnlyThatUsersRows()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        db.Organizations.Add(organization);
        var userA = CreateUser(organization.Id, "30111222", "jose.diaz@checkbus-demo.com");
        var userB = CreateUser(organization.Id, "30111333", "maria.gomez@checkbus-demo.com");
        db.Users.AddRange(userA, userB);
        db.DriverRequirements.AddRange(
            CreateRequirement(userA.Id, DriverRequirementType.LicenciaConducir),
            CreateRequirement(userA.Id, DriverRequirementType.CapacitacionProfesional),
            CreateRequirement(userB.Id, DriverRequirementType.LicenciaConducir));
        await db.SaveChangesAsync(cancellationToken);

        var repository = new DriverRequirementRepository(db);

        var requirements = await repository.GetByUserIdAsync(userA.Id, cancellationToken);

        Assert.Equal(2, requirements.Count);
        Assert.All(requirements, r => Assert.Equal(userA.Id, r.UserId));
    }

    [Fact]
    public async Task UniqueIndex_DuplicateUserIdAndType_IsRejectedByUniqueIndex()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        db.Organizations.Add(organization);
        var user = CreateUser(organization.Id, "30111222", "jose.diaz@checkbus-demo.com");
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new DriverRequirementRepository(db);
        await repository.AddRangeAsync(
            [CreateRequirement(user.Id, DriverRequirementType.LicenciaConducir)], cancellationToken);

        await Assert.ThrowsAsync<DbUpdateException>(() => repository.AddRangeAsync(
            [CreateRequirement(user.Id, DriverRequirementType.LicenciaConducir)], cancellationToken));
    }

    [Fact]
    public async Task GetExpiringOrExpiredCountByOrganizationAsync_CountsOnlySameOrganizationRowsWithinThreshold()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organizationA = CreateOrganization("org-a");
        var organizationB = CreateOrganization("org-b");
        db.Organizations.AddRange(organizationA, organizationB);

        var userA = CreateUser(organizationA.Id, "30111222", "jose.diaz@org-a.com");
        var userB = CreateUser(organizationB.Id, "30111333", "maria.gomez@org-b.com");
        db.Users.AddRange(userA, userB);

        var today = new DateOnly(2026, 10, 2);
        var threshold = today.AddDays(30);

        db.DriverRequirements.AddRange(
            // Org A, already expired -> counted.
            CreateRequirement(userA.Id, DriverRequirementType.LicenciaConducir, expirationDate: today.AddDays(-5)),
            // Org A, expires exactly at threshold -> counted.
            CreateRequirement(userA.Id, DriverRequirementType.CapacitacionProfesional, expirationDate: threshold),
            // Org B, same expiring window -> excluded (different organization).
            CreateRequirement(userB.Id, DriverRequirementType.LicenciaConducir, expirationDate: today.AddDays(-5)));
        await db.SaveChangesAsync(cancellationToken);

        // Org A, no expiration date at all -> excluded (added after save to avoid affecting
        // the unique index on userA, since LicenciaConducir/CapacitacionProfesional are used above).
        var userA2 = CreateUser(organizationA.Id, "30111444", "pedro.lopez@org-a.com");
        db.Users.Add(userA2);
        db.DriverRequirements.AddRange(
            CreateRequirement(userA2.Id, DriverRequirementType.LicenciaConducir, expirationDate: null),
            // Org A, expires well beyond the threshold -> excluded.
            CreateRequirement(userA2.Id, DriverRequirementType.CapacitacionProfesional, expirationDate: threshold.AddDays(1)));
        await db.SaveChangesAsync(cancellationToken);

        var repository = new DriverRequirementRepository(db);

        var count = await repository.GetExpiringOrExpiredCountByOrganizationAsync(organizationA.Id, threshold, cancellationToken);

        Assert.Equal(2, count);
    }
}
