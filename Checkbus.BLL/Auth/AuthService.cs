using Checkbus.BEL.Auth;
using Checkbus.DAL.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.BLL.Auth;

/// <summary>
/// Default <see cref="IAuthService"/>. Looks the user up by e-mail with
/// <c>IgnoreQueryFilters</c> (there is no tenant context yet before sign-in —
/// the tenant is *resolved by* this lookup, not required by it) and verifies
/// the password with the framework's non-EF <see cref="PasswordHasher{TUser}"/>,
/// deliberately avoiding a parallel ASP.NET Core Identity user store: the
/// project's own <see cref="User"/> entity remains the single source of truth
/// (single-database design decision), and only the hashing primitive is reused.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IDbContextFactory<CheckbusDbContext> _contextFactory;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthService(IDbContextFactory<CheckbusDbContext> contextFactory, IPasswordHasher<User> passwordHasher)
    {
        _contextFactory = contextFactory;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthenticatedUser?> AuthenticateAsync(string email, string password)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var user = await context.Users
            .IgnoreQueryFilters()
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.Email == email);

        if (user is null || !user.IsActive)
        {
            return null;
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return new AuthenticatedUser(user.Id, user.OrganizationId, user.Email, user.FullName, user.RoleId, user.Role.Name);
    }
}
