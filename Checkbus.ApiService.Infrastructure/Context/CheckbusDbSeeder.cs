using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Authentication.Authorization;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Infrastructure.Implementations.Authentication;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Persistence
{
    /// <summary>
    /// Seeding hooks for <see cref="CheckbusDbContext"/>, wired via
    /// <c>DbContextOptionsBuilder.UseSeeding</c>/<c>UseAsyncSeeding</c>.
    /// Converges the database on the canonical <see cref="Roles"/> set, the demo
    /// organization, and the seeded admin user on every startup — idempotent and
    /// convergent regardless of the database's previous state (fresh, already
    /// seeded, or half-migrated). The <c>isPopulated</c> parameter required by the
    /// EF Core seeding delegate signatures is intentionally ignored: only the
    /// database's actual contents decide what, if anything, still needs staging.
    /// </summary>
    public static class CheckbusDbSeeder
    {
        private const string SeededAdminEmail = "admin@checkbus.dev";
        private const string SeededAdminUsername = "admin";
        private const string SeededAdminPassword = "Admin123!";
        private const string DemoOrganizationSlug = "checkbus-demo";
        private const string LegacyAdminRoleName = "Admin";

        public static void Seed(DbContext context, bool isPopulated)
        {
            var db = (CheckbusDbContext)context;

            if (!StageSeed(db)) return;

            db.SaveChanges();
        }

        public static async Task SeedAsync(DbContext context, bool isPopulated, CancellationToken cancellationToken)
        {
            var db = (CheckbusDbContext)context;

            if (!StageSeed(db)) return;

            await db.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Queries and stages the canonical roles, demo organization, and seeded
        /// admin user against <paramref name="db"/>. Never calls <c>SaveChanges</c>
        /// — callers decide whether to save synchronously or asynchronously.
        /// </summary>
        /// <returns><c>true</c> when staging produced pending changes.</returns>
        private static bool StageSeed(CheckbusDbContext db)
        {
            // 1. Roles — rename in place, never recreate, so an existing FK
            // reference to the renamed row's Id never needs repointing.
            var roles = db.Roles.ToList();
            var legacyAdminRole = roles.FirstOrDefault(r => r.Name == LegacyAdminRoleName);
            var administradorRole = roles.FirstOrDefault(r => r.Name == Roles.Administrador);

            if (legacyAdminRole is not null && administradorRole is null)
            {
                legacyAdminRole.Name = Roles.Administrador;
                administradorRole = legacyAdminRole;
            }
            else if (legacyAdminRole is not null && administradorRole is not null)
            {
                // Both rows exist (e.g. a half-migrated database). Repoint every
                // user still on the legacy role, then delete it — the repoint and
                // the delete are staged in the same ChangeTracker, so SaveChanges
                // orders the UPDATE before the DELETE and no FK violation occurs.
                foreach (var user in db.Users.Where(u => u.RoleId == legacyAdminRole.Id).ToList())
                {
                    user.RoleId = administradorRole.Id;
                }

                db.Roles.Remove(legacyAdminRole);
                roles.Remove(legacyAdminRole);
            }

            var existingRoleNames = roles.Select(r => r.Name).ToHashSet(StringComparer.Ordinal);

            foreach (var roleName in Roles.All)
            {
                if (existingRoleNames.Contains(roleName)) continue;

                var newRole = new Role { Id = Guid.NewGuid(), Name = roleName };
                db.Roles.Add(newRole);
                existingRoleNames.Add(roleName);

                if (roleName == Roles.Administrador)
                {
                    administradorRole = newRole;
                }
            }

            // 2. Organization
            var organization = db.Organizations.FirstOrDefault(o => o.Slug == DemoOrganizationSlug);
            if (organization is null)
            {
                organization = new Organization
                {
                    Id = Guid.NewGuid(),
                    CUIT = "20-12345678-9",
                    Name = "Checkbus Demo",
                    Slug = DemoOrganizationSlug,
                    LogoUrl = "",
                    IsActive = true
                };
                db.Organizations.Add(organization);
            }

            // 3. Admin user — Roles.All always includes Administrador, so by this
            // point administradorRole is guaranteed to be resolved.
            var administradorRoleId = administradorRole!.Id;
            var adminUser = db.Users.FirstOrDefault(u => u.Email == SeededAdminEmail);

            if (adminUser is null)
            {
                var hasher = new IdentityPasswordHasher();
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = SeededAdminUsername,
                    Email = SeededAdminEmail,
                    PasswordHash = string.Empty,
                    DocumentNumber = "12345678",
                    RoleId = administradorRoleId,
                    Role = administradorRole,
                    OrganizationId = organization.Id,
                    Organization = organization,
                    IsActive = true
                };
                user.PasswordHash = hasher.Hash(user, SeededAdminPassword);

                db.Users.Add(user);
            }
            else if (adminUser.RoleId != administradorRoleId)
            {
                adminUser.RoleId = administradorRoleId;
            }

            return db.ChangeTracker.HasChanges();
        }
    }
}
