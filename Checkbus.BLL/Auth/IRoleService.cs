using Checkbus.BEL.Auth;

namespace Checkbus.BLL.Auth;

/// <summary>
/// Custom-role CRUD scoped to a single organization. Predefined (global)
/// roles are read-only from this service: update/delete attempts on them
/// are rejected with a clear <see cref="InvalidOperationException"/> rather
/// than only surfacing the lower-level DbContext <c>SaveChanges</c> guard.
/// </summary>
public interface IRoleService
{
    Task<Role> CreateCustomRoleAsync(int organizationId, string name);

    /// <exception cref="InvalidOperationException">The role is predefined (global).</exception>
    Task UpdateCustomRoleAsync(int roleId, string newName);

    /// <exception cref="InvalidOperationException">The role is predefined (global).</exception>
    Task DeleteCustomRoleAsync(int roleId);
}
