using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Infrastructure.Implementations.Authentication;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Persistence
{
    /// <summary>
    /// Seeding hooks for <see cref="CheckbusDbContext"/>, wired via
    /// <c>DbContextOptionsBuilder.UseSeeding</c>/<c>UseAsyncSeeding</c>.
    /// Ensures the demo organization and the seeded admin user exist on every
    /// startup — idempotent and convergent regardless of the database's
    /// previous state. Roles are a fixed set of string values (see
    /// <see cref="Roles"/>), not a database table, so there is nothing else to
    /// seed. The <c>isPopulated</c> parameter required by the EF Core seeding
    /// delegate signatures is intentionally ignored: only the database's
    /// actual contents decide what, if anything, still needs staging.
    /// </summary>
    public static class CheckbusDbSeeder
    {
        private const string SeededAdminEmail = "admin@checkbus.dev";
        private const string SeededAdminUsername = "admin";
        private const string SeededAdminPassword = "Admin123!";
        private const string DemoOrganizationSlug = "checkbus-demo";

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
        /// Queries and stages the demo organization and the seeded admin user
        /// against <paramref name="db"/>. Never calls <c>SaveChanges</c> —
        /// callers decide whether to save synchronously or asynchronously.
        /// </summary>
        /// <returns><c>true</c> when staging produced pending changes.</returns>
        private static bool StageSeed(CheckbusDbContext db)
        {
            // 1. Organization
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

            // 2. Admin user
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
                    Role = Roles.Administrador,
                    OrganizationId = organization.Id,
                    Organization = organization,
                    IsActive = true
                };
                user.PasswordHash = hasher.Hash(user, SeededAdminPassword);

                db.Users.Add(user);
            }
            else if (string.IsNullOrWhiteSpace(adminUser.Role))
            {
                // Only repair a genuinely invalid/empty stored value (e.g. left
                // over from a bug or a botched manual edit). Never overwrite a
                // valid, different role: that could be a deliberate operator
                // decision (e.g. demoting this well-known seeded account), and
                // seeding must not silently reverse it.
                adminUser.Role = Roles.Administrador;
            }

            return db.ChangeTracker.HasChanges();
        }
    }
}
