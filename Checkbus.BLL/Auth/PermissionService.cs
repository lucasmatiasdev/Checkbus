using Checkbus.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.BLL.Auth;

/// <summary>
/// Default <see cref="IPermissionService"/>, resolving grants through
/// <c>User -&gt; Role -&gt; RolePermission -&gt; Permission</c> via a fresh
/// <see cref="CheckbusDbContext"/> per call.
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly IDbContextFactory<CheckbusDbContext> _contextFactory;

    public PermissionService(IDbContextFactory<CheckbusDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<bool> HasPermissionAsync(int userId, string permissionKey)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Users
            .Where(u => u.Id == userId)
            .SelectMany(u => u.Role.RolePermissions)
            .AnyAsync(rp => rp.Permission.Key == permissionKey);
    }
}
