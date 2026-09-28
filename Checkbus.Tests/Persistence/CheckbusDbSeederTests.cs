using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Authentication.Authorization;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Persistence;

public class CheckbusDbSeederTests
{
    private const string SeededAdminEmail = "admin@checkbus.dev";
    private const string DemoOrganizationSlug = "checkbus-demo";

    private static SqliteConnection CreateOpenConnection()
    {
        // "Foreign Keys=True" makes Microsoft.Data.Sqlite issue PRAGMA foreign_keys=1
        // on open, so the convergence branch's repoint-before-delete is actually
        // enforced by the database, not merely assumed.
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

    private static Organization CreateOrganization() => new()
    {
        Id = Guid.NewGuid(),
        CUIT = "20-12345678-9",
        Name = "Checkbus Demo",
        Slug = DemoOrganizationSlug,
        LogoUrl = "",
        IsActive = true
    };

    [Fact]
    public void Seed_FreshDatabase_SeedsExactlyCanonicalRoles()
    {
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        CheckbusDbSeeder.Seed(db, isPopulated: false);

        var roleNames = db.Roles.Select(r => r.Name).ToList();
        Assert.Equal(Roles.All.Count, roleNames.Count);
        foreach (var expected in Roles.All)
        {
            Assert.Contains(expected, roleNames);
        }
        Assert.DoesNotContain("Admin", roleNames);
    }

    [Fact]
    public void Seed_FreshDatabase_AdminUserReferencesAdministradorRole()
    {
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        CheckbusDbSeeder.Seed(db, isPopulated: false);

        var adminUser = db.Users.Single(u => u.Email == SeededAdminEmail);
        var administradorRole = db.Roles.Single(r => r.Name == Roles.Administrador);
        Assert.Equal(administradorRole.Id, adminUser.RoleId);
    }

    [Fact]
    public void Seed_AlreadySeededDatabase_StagesNoChangesAndDoesNotDuplicate()
    {
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();
        CheckbusDbSeeder.Seed(db, isPopulated: false);

        var roleCountAfterFirstSeed = db.Roles.Count();
        var userCountAfterFirstSeed = db.Users.Count();

        CheckbusDbSeeder.Seed(db, isPopulated: false);

        Assert.False(db.ChangeTracker.HasChanges());
        Assert.Equal(roleCountAfterFirstSeed, db.Roles.Count());
        Assert.Equal(userCountAfterFirstSeed, db.Users.Count());
    }

    [Fact]
    public async Task SeedAsync_AlreadySeededDatabase_StagesNoChangesAndDoesNotDuplicate()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();
        await CheckbusDbSeeder.SeedAsync(db, isPopulated: false, cancellationToken);

        var roleCountAfterFirstSeed = db.Roles.Count();
        var userCountAfterFirstSeed = db.Users.Count();

        await CheckbusDbSeeder.SeedAsync(db, isPopulated: false, cancellationToken);

        Assert.False(db.ChangeTracker.HasChanges());
        Assert.Equal(roleCountAfterFirstSeed, db.Roles.Count());
        Assert.Equal(userCountAfterFirstSeed, db.Users.Count());
    }

    [Fact]
    public void Seed_OnlyLegacyAdminRoleExists_RenamesInPlacePreservingId()
    {
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization();
        var legacyRole = new Role { Id = Guid.NewGuid(), Name = "Admin" };
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Email = SeededAdminEmail,
            PasswordHash = "existing-hash",
            DocumentNumber = "12345678",
            RoleId = legacyRole.Id,
            Role = legacyRole,
            OrganizationId = organization.Id,
            Organization = organization,
            IsActive = true
        };
        db.Organizations.Add(organization);
        db.Roles.Add(legacyRole);
        db.Users.Add(adminUser);
        db.SaveChanges();
        var legacyRoleId = legacyRole.Id;

        CheckbusDbSeeder.Seed(db, isPopulated: false);

        var roles = db.Roles.ToList();
        Assert.DoesNotContain(roles, r => r.Name == "Admin");
        var administradorRole = Assert.Single(roles, r => r.Name == Roles.Administrador);
        Assert.Equal(legacyRoleId, administradorRole.Id);

        var reloadedUser = db.Users.Single(u => u.Email == SeededAdminEmail);
        Assert.Equal(legacyRoleId, reloadedUser.RoleId);
    }

    [Fact]
    public void Seed_BothLegacyAndCanonicalAdminRolesExist_RepointsUsersThenDeletesLegacyRole()
    {
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization();
        var legacyRole = new Role { Id = Guid.NewGuid(), Name = "Admin" };
        var administradorRole = new Role { Id = Guid.NewGuid(), Name = Roles.Administrador };
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Email = SeededAdminEmail,
            PasswordHash = "existing-hash",
            DocumentNumber = "12345678",
            RoleId = legacyRole.Id,
            Role = legacyRole,
            OrganizationId = organization.Id,
            Organization = organization,
            IsActive = true
        };
        db.Organizations.Add(organization);
        db.Roles.AddRange(legacyRole, administradorRole);
        db.Users.Add(adminUser);
        db.SaveChanges();
        var administradorRoleId = administradorRole.Id;

        var exception = Record.Exception(() => CheckbusDbSeeder.Seed(db, isPopulated: false));

        Assert.Null(exception);
        var roles = db.Roles.ToList();
        Assert.DoesNotContain(roles, r => r.Name == "Admin");
        Assert.Single(roles, r => r.Name == Roles.Administrador);

        var reloadedUser = db.Users.Single(u => u.Email == SeededAdminEmail);
        Assert.Equal(administradorRoleId, reloadedUser.RoleId);
    }

    [Fact]
    public void Seed_AdminUserRoleIdOrphaned_RepairsToAdministrador()
    {
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();
        CheckbusDbSeeder.Seed(db, isPopulated: false);

        // Simulate data corruption: RoleId references no row at all (not a
        // deliberate reassignment to a real role — see the sibling test below).
        // FKs are enforced, so this can only happen via a bypass, exactly like
        // real-world corruption from raw SQL or a pre-FK-enforcement bug would.
        var adminUser = db.Users.Single(u => u.Email == SeededAdminEmail);
        adminUser.RoleId = Guid.NewGuid();
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys=OFF");
        db.SaveChanges();
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys=ON");

        CheckbusDbSeeder.Seed(db, isPopulated: false);

        var administradorRole = db.Roles.Single(r => r.Name == Roles.Administrador);
        var reloadedUser = db.Users.Single(u => u.Email == SeededAdminEmail);
        Assert.Equal(administradorRole.Id, reloadedUser.RoleId);
    }

    [Fact]
    public void Seed_AdminUserReassignedToRealRole_DoesNotRevertReassignment()
    {
        // Anti-regression for a corrected security finding: seeding must never
        // silently undo a deliberate operator action (e.g. demoting this
        // well-known seeded account) just because it no longer matches
        // Administrador. Only a truly orphaned RoleId gets repaired.
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();
        CheckbusDbSeeder.Seed(db, isPopulated: false);

        var choferRole = db.Roles.Single(r => r.Name == Roles.Chofer);
        var adminUser = db.Users.Single(u => u.Email == SeededAdminEmail);
        adminUser.RoleId = choferRole.Id;
        db.SaveChanges();

        CheckbusDbSeeder.Seed(db, isPopulated: false);

        var reloadedUser = db.Users.Single(u => u.Email == SeededAdminEmail);
        Assert.Equal(choferRole.Id, reloadedUser.RoleId);
    }

    [Fact]
    public void Seed_ExistingAdminUser_LeavesPasswordHashUntouched()
    {
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();
        CheckbusDbSeeder.Seed(db, isPopulated: false);

        var adminUser = db.Users.Single(u => u.Email == SeededAdminEmail);
        adminUser.PasswordHash = "developer-changed-this-password-hash";
        db.SaveChanges();

        CheckbusDbSeeder.Seed(db, isPopulated: false);

        var reloadedUser = db.Users.Single(u => u.Email == SeededAdminEmail);
        Assert.Equal("developer-changed-this-password-hash", reloadedUser.PasswordHash);
    }

    [Fact]
    public async Task Seed_And_SeedAsync_ProduceIdenticalStateFromEmptyDatabases()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        using var syncConnection = CreateOpenConnection();
        using var syncDb = CreateContext(syncConnection);
        syncDb.Database.EnsureCreated();
        CheckbusDbSeeder.Seed(syncDb, isPopulated: false);

        using var asyncConnection = CreateOpenConnection();
        using var asyncDb = CreateContext(asyncConnection);
        asyncDb.Database.EnsureCreated();
        await CheckbusDbSeeder.SeedAsync(asyncDb, isPopulated: false, cancellationToken);

        var syncRoleNames = syncDb.Roles.Select(r => r.Name).ToList().OrderBy(n => n, StringComparer.Ordinal).ToList();
        var asyncRoleNames = asyncDb.Roles.Select(r => r.Name).ToList().OrderBy(n => n, StringComparer.Ordinal).ToList();
        Assert.Equal(syncRoleNames, asyncRoleNames);

        var syncAdminRoleName = syncDb.Users
            .Where(u => u.Email == SeededAdminEmail)
            .Join(syncDb.Roles, u => u.RoleId, r => r.Id, (u, r) => r.Name)
            .Single();
        var asyncAdminRoleName = asyncDb.Users
            .Where(u => u.Email == SeededAdminEmail)
            .Join(asyncDb.Roles, u => u.RoleId, r => r.Id, (u, r) => r.Name)
            .Single();
        Assert.Equal(syncAdminRoleName, asyncAdminRoleName);
        Assert.Equal(Roles.Administrador, syncAdminRoleName);
    }
}
