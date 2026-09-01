using Checkbus.BEL.Auth;
using Checkbus.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.BLL.Auth;

/// <summary>
/// Default <see cref="IRoleService"/>. Update/delete reject predefined roles
/// at the service boundary with a clear message, ahead of the lower-level
/// <c>CheckbusDbContext.SaveChanges</c> immutability guard from PR1 (which
/// still applies as a defense-in-depth safety net for any other write path).
/// </summary>
public class RoleService : IRoleService
{
    private readonly IDbContextFactory<CheckbusDbContext> _contextFactory;

    public RoleService(IDbContextFactory<CheckbusDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Role> CreateCustomRoleAsync(int organizationId, string name)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var role = new Role { Name = name, OrganizationId = organizationId };
        context.Roles.Add(role);
        await context.SaveChangesAsync();
        return role;
    }

    public async Task UpdateCustomRoleAsync(int roleId, string newName)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var role = await context.Roles.IgnoreQueryFilters().SingleAsync(r => r.Id == roleId);
        EnsureCustomRole(role);

        role.Name = newName;
        await context.SaveChangesAsync();
    }

    public async Task DeleteCustomRoleAsync(int roleId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var role = await context.Roles.IgnoreQueryFilters().SingleAsync(r => r.Id == roleId);
        EnsureCustomRole(role);

        context.Roles.Remove(role);
        await context.SaveChangesAsync();
    }

    private static void EnsureCustomRole(Role role)
    {
        if (role.OrganizationId is null)
        {
            throw new InvalidOperationException(
                $"Role '{role.Name}' (Id={role.Id}) is a predefined, global role and cannot be updated or deleted.");
        }
    }
}
