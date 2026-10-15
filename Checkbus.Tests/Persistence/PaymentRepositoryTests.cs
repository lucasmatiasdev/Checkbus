using Checkbus.ApiService.Domain.Entities.Payments;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.ValueObjects;
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Persistence;

/// <summary>
/// Proves <see cref="PaymentRepository"/> (odd/tasks/pagos-mercadopago.md Fase B, B5): plain
/// CRUD round-trips plus <see cref="PaymentRepository.GetByExternalOrderIdAsync"/>, the lookup
/// Fase D's webhook handler will use to resolve a notification back to a <see cref="Payment"/>
/// row. Same SQLite-in-memory setup as every other persistence test in this folder.
/// </summary>
public class PaymentRepositoryTests
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

    private static Payment CreatePayment() => new()
    {
        Id = Guid.NewGuid(),
        TotalAmount = new Money(1500m),
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTrips()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new PaymentRepository(db);
        var payment = CreatePayment();

        await repository.AddAsync(payment, cancellationToken);
        var result = await repository.GetByIdAsync(payment.Id, cancellationToken);

        Assert.NotNull(result);
        Assert.Equal(payment.TotalAmount.Amount, result!.TotalAmount.Amount);
        Assert.Equal(PaymentStatus.Pendiente, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new PaymentRepository(db);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), cancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByExternalOrderIdAsync_ResolvesTheMatchingPayment()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new PaymentRepository(db);
        var payment = CreatePayment();
        payment.ExternalOrderId = "mp-order-abc";
        await repository.AddAsync(payment, cancellationToken);
        await repository.AddAsync(CreatePayment(), cancellationToken); // a decoy with no match

        var result = await repository.GetByExternalOrderIdAsync("mp-order-abc", cancellationToken);

        Assert.NotNull(result);
        Assert.Equal(payment.Id, result!.Id);
    }

    [Fact]
    public async Task GetByExternalOrderIdAsync_UnknownOrderId_ReturnsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new PaymentRepository(db);

        var result = await repository.GetByExternalOrderIdAsync("does-not-exist", cancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_PersistsStatusAndExternalOrderIdChanges()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new PaymentRepository(db);
        var payment = CreatePayment();
        await repository.AddAsync(payment, cancellationToken);

        payment.ExternalOrderId = "mp-order-updated";
        payment.Status = PaymentStatus.Aprobado;
        await repository.UpdateAsync(payment, cancellationToken);

        var reloaded = await db.Payments.FirstAsync(p => p.Id == payment.Id, cancellationToken);
        Assert.Equal("mp-order-updated", reloaded.ExternalOrderId);
        Assert.Equal(PaymentStatus.Aprobado, reloaded.Status);
    }
}
