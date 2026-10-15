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
    /// previous state. Roles are a fixed <see cref="Role"/> enum, not a
    /// database table, so there is nothing else to seed. The
    /// <c>isPopulated</c> parameter required by the EF Core seeding delegate
    /// signatures is intentionally ignored: only the database's actual
    /// contents decide what, if anything, still needs staging.
    /// </summary>
    public static class CheckbusDbSeeder
    {
        private const string SeededAdminEmail = "admin@checkbus.dev";
        private const string SeededAdminPassword = "Admin123!";
        private const string DemoOrganizationSlug = "checkbus-demo";

        private const string BrahmanAdminEmail = "admin@brahman.dev";
        private const string BrahmanAdminPassword = "Admin123!";
        private const string BrahmanOrganizationSlug = "brahman";

        private const string CheckbusInternalAdminEmail = "soporte@checkbus.dev";
        private const string CheckbusInternalAdminPassword = "Soporte123!";

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
        /// Queries and stages each seeded organization and its admin user
        /// against <paramref name="db"/>. Never calls <c>SaveChanges</c> —
        /// callers decide whether to save synchronously or asynchronously.
        /// </summary>
        /// <returns><c>true</c> when staging produced pending changes.</returns>
        private static bool StageSeed(CheckbusDbContext db)
        {
            StageOrganizationWithAdmin(
                db,
                slug: DemoOrganizationSlug,
                organizationName: "Checkbus Demo",
                cuit: "20-12345678-9",
                adminEmail: SeededAdminEmail,
                adminPassword: SeededAdminPassword,
                adminName: "Admin",
                adminSurname: "Checkbus",
                adminDocumentNumber: "12345678");

            StageOrganizationWithAdmin(
                db,
                slug: BrahmanOrganizationSlug,
                organizationName: "Brahman",
                cuit: "20-87654321-3",
                adminEmail: BrahmanAdminEmail,
                adminPassword: BrahmanAdminPassword,
                adminName: "Admin",
                adminSurname: "Brahman",
                adminDocumentNumber: "87654321");

            StageOrganizationWithAdmin(
                db,
                slug: CheckbusInternalOrganization.Slug,
                organizationName: "Checkbus Interno",
                cuit: "20-00000000-0",
                adminEmail: CheckbusInternalAdminEmail,
                adminPassword: CheckbusInternalAdminPassword,
                adminName: "Soporte",
                adminSurname: "Checkbus",
                adminDocumentNumber: "00000000",
                role: Role.CheckbusSoporte);

            return db.ChangeTracker.HasChanges();
        }

        /// <summary>
        /// Stages one organization identified by <paramref name="slug"/> and its
        /// admin user identified by <paramref name="adminEmail"/>, creating
        /// whichever of the two is missing. If the admin user already exists,
        /// its <see cref="User.Role"/> is left untouched entirely: Role is an
        /// enum, so it cannot hold an empty/invalid value through normal
        /// construction, and there is nothing left to repair. This also means
        /// seeding can never silently revert a deliberate operator
        /// reassignment to a different valid role (e.g. demoting a
        /// well-known seeded account) — there is no conditional branch that
        /// could do so.
        /// </summary>
        private static void StageOrganizationWithAdmin(
            CheckbusDbContext db,
            string slug,
            string organizationName,
            string cuit,
            string adminEmail,
            string adminPassword,
            string adminName,
            string adminSurname,
            string adminDocumentNumber,
            Role role = Role.Administrador)
        {
            var organization = db.Organizations.FirstOrDefault(o => o.Slug == slug);
            if (organization is null)
            {
                organization = new Organization
                {
                    Id = Guid.NewGuid(),
                    CUIT = cuit,
                    Name = organizationName,
                    Slug = slug,
                    LogoUrl = "",
                    IsActive = true
                };
                db.Organizations.Add(organization);
            }

            var adminUser = db.Users.FirstOrDefault(u => u.Email == adminEmail);

            if (adminUser is null)
            {
                var hasher = new IdentityPasswordHasher();
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Name = adminName,
                    Surname = adminSurname,
                    Email = adminEmail,
                    PasswordHash = string.Empty,
                    DocumentNumber = adminDocumentNumber,
                    Role = role,
                    OrganizationId = organization.Id,
                    Organization = organization,
                    IsActive = true
                };
                user.PasswordHash = hasher.Hash(user, adminPassword);

                db.Users.Add(user);
            }
        }
    }
}
