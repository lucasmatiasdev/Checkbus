using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Audit;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Persistence;

public class AuditLogRepositoryTests
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
        Role = Role.Planificador,
        OrganizationId = organizationId,
        IsActive = true
    };

    private static AuditLogEntry CreateEntry(Guid? organizationId, Guid? actorUserId, string actorEmail, AuditAction action, DateTime createdAt) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
        ActorUserId = actorUserId,
        ActorEmail = actorEmail,
        Action = action,
        EntityType = "User",
        EntityId = Guid.NewGuid(),
        Description = "Inicio de sesion exitoso",
        CreatedAt = createdAt
    };

    [Fact]
    public async Task AddAsync_PersistsEntryWithNullableOrganizationAndActor()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new AuditLogRepository(db);
        // No Organization/User row created on purpose: a failed login with an unknown email has
        // no OrganizationId/ActorUserId to attribute, only the attempted ActorEmail.
        var entry = CreateEntry(null, null, "unknown@checkbus-demo.com", AuditAction.LoginFallido, DateTime.UtcNow);

        await repository.AddAsync(entry, cancellationToken);
        var result = await repository.GetAllAsync(cancellationToken);

        Assert.Single(result);
        Assert.Null(result[0].OrganizationId);
        Assert.Null(result[0].ActorUserId);
        Assert.Equal(AuditAction.LoginFallido, result[0].Action);
    }

    [Fact]
    public async Task GetAllByOrganizationAsync_ReturnsOnlyThatOrganizationsEntries_OrderedByCreatedAtDescending()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organizationA = CreateOrganization("checkbus-demo");
        var organizationB = CreateOrganization("brahman");
        var userA = CreateUser(organizationA.Id, "30111222", "jose@checkbus-demo.com");
        var userB = CreateUser(organizationB.Id, "30333444", "ana@brahman.com");
        db.Organizations.AddRange(organizationA, organizationB);
        db.Users.AddRange(userA, userB);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new AuditLogRepository(db);
        var now = DateTime.UtcNow;
        var oldest = CreateEntry(organizationA.Id, userA.Id, userA.Email, AuditAction.LoginExitoso, now.AddDays(-2));
        var newest = CreateEntry(organizationA.Id, userA.Id, userA.Email, AuditAction.ViajeCancelado, now.AddDays(-1));
        var otherOrgEntry = CreateEntry(organizationB.Id, userB.Id, userB.Email, AuditAction.LoginExitoso, now);
        await repository.AddAsync(oldest, cancellationToken);
        await repository.AddAsync(newest, cancellationToken);
        await repository.AddAsync(otherOrgEntry, cancellationToken);

        var result = await repository.GetAllByOrganizationAsync(organizationA.Id, cancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Equal(newest.Id, result[0].Id);
        Assert.Equal(oldest.Id, result[1].Id);
        Assert.All(result, e => Assert.Equal(organizationA.Id, e.OrganizationId));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEntriesAcrossAllOrganizations()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organizationA = CreateOrganization("checkbus-demo");
        var organizationB = CreateOrganization("brahman");
        var userA = CreateUser(organizationA.Id, "30111222", "jose@checkbus-demo.com");
        var userB = CreateUser(organizationB.Id, "30333444", "ana@brahman.com");
        db.Organizations.AddRange(organizationA, organizationB);
        db.Users.AddRange(userA, userB);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new AuditLogRepository(db);
        await repository.AddAsync(CreateEntry(organizationA.Id, userA.Id, userA.Email, AuditAction.LoginExitoso, DateTime.UtcNow), cancellationToken);
        await repository.AddAsync(CreateEntry(organizationB.Id, userB.Id, userB.Email, AuditAction.LoginExitoso, DateTime.UtcNow), cancellationToken);

        var result = await repository.GetAllAsync(cancellationToken);

        Assert.Equal(2, result.Count);
    }
}
