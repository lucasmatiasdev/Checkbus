using Checkbus.DAL.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Integration;

/// <summary>
/// Shared SQLite in-memory helpers for RBAC/multi-tenant integration tests.
/// A single open <see cref="SqliteConnection"/> backs an in-memory database
/// that lives for the lifetime of the connection, so multiple
/// <see cref="CheckbusDbContext"/> instances (one per simulated tenant
/// request) can all see the same schema and data.
/// </summary>
public static class SqliteDbContextTestHelper
{
    public static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    public static CheckbusDbContext CreateContext(SqliteConnection connection, ITenantProvider tenantProvider)
    {
        var options = new DbContextOptionsBuilder<CheckbusDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new CheckbusDbContext(options);
        context.SetTenantProvider(tenantProvider);
        return context;
    }
}
