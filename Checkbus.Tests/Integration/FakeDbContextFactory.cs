using Checkbus.DAL.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Integration;

/// <summary>
/// Test double for <see cref="IDbContextFactory{TContext}"/> that hands out
/// <see cref="CheckbusDbContext"/> instances backed by a shared, already-open
/// SQLite in-memory connection and a fixed <see cref="ITenantProvider"/>, so
/// BLL services under test see the same simulated tenant on every call.
/// </summary>
public class FakeDbContextFactory : IDbContextFactory<CheckbusDbContext>
{
    private readonly SqliteConnection _connection;
    private readonly ITenantProvider _tenantProvider;

    public FakeDbContextFactory(SqliteConnection connection, ITenantProvider tenantProvider)
    {
        _connection = connection;
        _tenantProvider = tenantProvider;
    }

    public CheckbusDbContext CreateDbContext() =>
        SqliteDbContextTestHelper.CreateContext(_connection, _tenantProvider);

    public Task<CheckbusDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CreateDbContext());
}
