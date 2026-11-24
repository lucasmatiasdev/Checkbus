using Checkbus.ApiService.Domain.Entities.Payments;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Persistence;

/// <summary>
/// Proves <see cref="OrganizationMercadoPagoConnectionRepository"/> (odd/tasks/pagos-mercadopago.md
/// Fase I, I1/I4): insert/update ("upsert") behavior, the unique-per-Organization DB constraint,
/// and — the point of this Fase's encryption requirement — that a value written through the
/// repository comes back DECRYPTED via <c>GetByOrganizationIdAsync</c> while the raw DB column
/// never holds the plaintext token. Uses <see cref="EphemeralDataProtectionProvider"/> (ships in
/// the <c>Microsoft.AspNetCore.DataProtection</c> package itself, purpose-built for exactly this:
/// a self-contained, in-memory-keyed provider with no ASP.NET Core host/key-ring setup needed —
/// same SQLite-in-memory convention as every other persistence test in this folder otherwise.
/// </summary>
public class OrganizationMercadoPagoConnectionRepositoryTests
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

    private static Organization CreateOrganization() => new()
    {
        Id = Guid.NewGuid(),
        CUIT = "20-12345678-9",
        Name = "Empresa de Buses S.A.",
        Slug = Guid.NewGuid().ToString("N"),
        LogoUrl = "https://example.com/logo.png"
    };

    private static MercadoPagoConnection CreateConnection(Guid organizationId) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
        SellerUserId = "123456789",
        AccessToken = "APP_USR-plaintext-access-token",
        RefreshToken = "TG-plaintext-refresh-token",
        ExpiresAt = DateTime.UtcNow.AddDays(180),
        ConnectedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task GetByOrganizationIdAsync_NoConnection_ReturnsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new OrganizationMercadoPagoConnectionRepository(db, new EphemeralDataProtectionProvider());

        var result = await repository.GetByOrganizationIdAsync(Guid.NewGuid(), cancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpsertAsync_ThenGetByOrganizationIdAsync_RoundTripsDecryptedTokens()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization();
        db.Organizations.Add(organization);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new OrganizationMercadoPagoConnectionRepository(db, new EphemeralDataProtectionProvider());
        var original = CreateConnection(organization.Id);

        await repository.UpsertAsync(original, cancellationToken);
        var result = await repository.GetByOrganizationIdAsync(organization.Id, cancellationToken);

        Assert.NotNull(result);
        Assert.Equal(original.AccessToken, result!.AccessToken);
        Assert.Equal(original.RefreshToken, result.RefreshToken);
        Assert.Equal(original.SellerUserId, result.SellerUserId);
    }

    [Fact]
    public async Task UpsertAsync_PersistsCiphertext_NeverThePlaintextTokenInTheRawColumn()
    {

        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization();
        db.Organizations.Add(organization);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new OrganizationMercadoPagoConnectionRepository(db, new EphemeralDataProtectionProvider());
        var original = CreateConnection(organization.Id);
        await repository.UpsertAsync(original, cancellationToken);

        using var rawDb = CreateContext(connection);
        var raw = await rawDb.MercadoPagoConnections.FirstAsync(c => c.OrganizationId == organization.Id, cancellationToken);

        Assert.NotEqual(original.AccessToken, raw.AccessToken);
        Assert.NotEqual(original.RefreshToken, raw.RefreshToken);
        Assert.DoesNotContain(original.AccessToken, raw.AccessToken);
        Assert.DoesNotContain(original.RefreshToken, raw.RefreshToken);
    }

    [Fact]
    public async Task UpsertAsync_CalledTwiceForSameOrganization_OverwritesRatherThanDuplicating()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization();
        db.Organizations.Add(organization);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new OrganizationMercadoPagoConnectionRepository(db, new EphemeralDataProtectionProvider());
        var first = CreateConnection(organization.Id);
        await repository.UpsertAsync(first, cancellationToken);

        var second = new MercadoPagoConnection
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            SellerUserId = "987654321",
            AccessToken = "APP_USR-new-access-token",
            RefreshToken = "TG-new-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddDays(180),
            ConnectedAt = DateTime.UtcNow
        };
        await repository.UpsertAsync(second, cancellationToken);

        Assert.Equal(1, await db.MercadoPagoConnections.CountAsync(c => c.OrganizationId == organization.Id, cancellationToken));

        var result = await repository.GetByOrganizationIdAsync(organization.Id, cancellationToken);
        Assert.NotNull(result);
        Assert.Equal("987654321", result!.SellerUserId);
        Assert.Equal("APP_USR-new-access-token", result.AccessToken);
        Assert.Equal("TG-new-refresh-token", result.RefreshToken);
    }

    [Fact]
    public async Task UniqueIndexOnOrganizationId_RejectsASecondRowInsertedDirectly()
    {

        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization();
        db.Organizations.Add(organization);
        await db.SaveChangesAsync(cancellationToken);

        db.MercadoPagoConnections.Add(CreateConnection(organization.Id));
        await db.SaveChangesAsync(cancellationToken);

        db.MercadoPagoConnections.Add(CreateConnection(organization.Id));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(cancellationToken));
    }
}
