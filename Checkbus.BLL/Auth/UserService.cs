using Checkbus.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.BLL.Auth;

/// <summary>
/// Default <see cref="IUserService"/>. Enforces the role-assignment scoping
/// invariant (Decision #28): a user may only be assigned a role that is
/// global (<c>OrganizationId == null</c>) or belongs to the user's own
/// organization. The role is looked up with <see cref="EntityFrameworkQueryableExtensions"/>
/// <c>IgnoreQueryFilters</c> so the check sees the role's real
/// <c>OrganizationId</c> regardless of the caller's current tenant filter,
/// rather than relying on the query filter to implicitly hide a
/// cross-tenant role (which would fail with a less clear "not found" error).
/// </summary>
public class UserService : IUserService
{
    private readonly IDbContextFactory<CheckbusDbContext> _contextFactory;

    public UserService(IDbContextFactory<CheckbusDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task AssignRoleAsync(int userId, int roleId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var user = await context.Users.SingleAsync(u => u.Id == userId);
        var role = await context.Roles.IgnoreQueryFilters().SingleAsync(r => r.Id == roleId);

        if (role.OrganizationId is not null && role.OrganizationId != user.OrganizationId)
        {
            throw new InvalidOperationException(
                $"Role {roleId} belongs to a different organization than user {userId}; cross-tenant role assignment is not allowed.");
        }

        user.RoleId = roleId;
        await context.SaveChangesAsync();
    }
}
