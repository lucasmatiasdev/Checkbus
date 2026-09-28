using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
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
    public void Seed_FreshDatabase_CreatesOrganizationAndAdminUserWithAdministradorRole()
    {
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        CheckbusDbSeeder.Seed(db, isPopulated: false);

        var organization = db.Organizations.Single(o => o.Slug == DemoOrganizationSlug);
        var adminUser = db.Users.Single(u => u.Email == SeededAdminEmail);
        Assert.Equal(Roles.Administrador, adminUser.Role);
        Assert.Equal(organization.Id, adminUser.OrganizationId);
    }

    [Fact]
    public void Seed_AlreadySeededDatabase_StagesNoChangesAndDoesNotDuplicate()
    {
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();
        CheckbusDbSeeder.Seed(db, isPopulated: false);

        var organizationCountAfterFirstSeed = db.Organizations.Count();
        var userCountAfterFirstSeed = db.Users.Count();

        CheckbusDbSeeder.Seed(db, isPopulated: false);

        Assert.False(db.ChangeTracker.HasChanges());
        Assert.Equal(organizationCountAfterFirstSeed, db.Organizations.Count());
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

        var organizationCountAfterFirstSeed = db.Organizations.Count();
        var userCountAfterFirstSeed = db.Users.Count();

        await CheckbusDbSeeder.SeedAsync(db, isPopulated: false, cancellationToken);

        Assert.False(db.ChangeTracker.HasChanges());
        Assert.Equal(organizationCountAfterFirstSeed, db.Organizations.Count());
        Assert.Equal(userCountAfterFirstSeed, db.Users.Count());
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
    public void Seed_AdminUserRoleEmpty_RepairsToAdministrador()
    {
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();
        CheckbusDbSeeder.Seed(db, isPopulated: false);

        // Simulate a genuinely invalid stored value (e.g. left over from a bug
        // or a botched manual edit) — not a deliberate operator reassignment.
        var adminUser = db.Users.Single(u => u.Email == SeededAdminEmail);
        adminUser.Role = string.Empty;
        db.SaveChanges();

        CheckbusDbSeeder.Seed(db, isPopulated: false);

        var reloadedUser = db.Users.Single(u => u.Email == SeededAdminEmail);
        Assert.Equal(Roles.Administrador, reloadedUser.Role);
    }

    [Fact]
    public void Seed_AdminUserReassignedToRealRole_DoesNotRevertReassignment()
    {
        // Anti-regression for a corrected security finding: seeding must never
        // silently undo a deliberate operator action (e.g. demoting this
        // well-known seeded account) just because it no longer matches
        // Administrador. Only a genuinely empty/invalid Role gets repaired.
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();
        CheckbusDbSeeder.Seed(db, isPopulated: false);

        var adminUser = db.Users.Single(u => u.Email == SeededAdminEmail);
        adminUser.Role = Roles.Chofer;
        db.SaveChanges();

        CheckbusDbSeeder.Seed(db, isPopulated: false);

        var reloadedUser = db.Users.Single(u => u.Email == SeededAdminEmail);
        Assert.Equal(Roles.Chofer, reloadedUser.Role);
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

        var syncAdminRole = syncDb.Users.Single(u => u.Email == SeededAdminEmail).Role;
        var asyncAdminRole = asyncDb.Users.Single(u => u.Email == SeededAdminEmail).Role;
        Assert.Equal(syncAdminRole, asyncAdminRole);
        Assert.Equal(Roles.Administrador, syncAdminRole);
    }
}
