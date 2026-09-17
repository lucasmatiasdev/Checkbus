using Checkbus.Application.Abstractions;
using Checkbus.Domain.Entities.Authentication;
using Checkbus.Domain.Entities.Authentication.Authorization;
using Checkbus.Domain.Entities.Tenancy;

namespace Checkbus.Infrastructure.Seeding
{
    /// <summary>
    /// Pure factory for the fixed Development seed literals (spec "Seed Data Shape and Tenant
    /// Coverage"): 2 tenant-isolated organizations, a minimal Administrator/Operator role set, 1
    /// tenant-agnostic global profile, and one org-scoped inactive user to exercise the
    /// "Inactive-user rejection path" scenario. Documented in `docs/dev-seed-data.md`.
    /// </summary>
    public static class DevelopmentSeedData
    {
        /// <summary>
        /// Shared plaintext password for every seeded user, hashed per-user via the injected
        /// <see cref="IPasswordHasher"/>. Documented alongside the seeded emails; Development-only.
        /// </summary>
        public const string SeedPassword = "Checkbus.Dev!2026";

        public static SeedGraph Build(IPasswordHasher passwordHasher)
        {
            var administratorRole = new Role { Name = "Administrator" };
            var operatorRole = new Role { Name = "Operator" };

            var orgNorte = new Organization { Name = "Checkbus Norte", CUIT = "20-11111111-1" };
            var orgSur = new Organization { Name = "Checkbus Sur", CUIT = "20-22222222-2" };

            // Tenant-agnostic: OrganizationId stays null so it is visible across every tenant
            // context (spec "Tenant Isolation Coverage").
            var globalProfile = new Profile
            {
                Name = "Global Support",
                OrganizationId = null,
                Roles = new List<Role> { administratorRole }
            };

            var norteAdminProfile = new Profile
            {
                Name = "Norte Administrator",
                Organization = orgNorte,
                Roles = new List<Role> { administratorRole }
            };
            var norteOperatorProfile = new Profile
            {
                Name = "Norte Operator",
                Organization = orgNorte,
                Roles = new List<Role> { operatorRole }
            };
            var surAdminProfile = new Profile
            {
                Name = "Sur Administrator",
                Organization = orgSur,
                Roles = new List<Role> { administratorRole }
            };

            var passwordHash = passwordHasher.Hash(SeedPassword);

            var norteAdmin = new User
            {
                FullName = "Norte Administrator",
                Email = "admin@seed-norte.test",
                PasswordHash = passwordHash,
                Organization = orgNorte,
                Profile = norteAdminProfile,
                DocumentNumber = "30111111",
                MustChangePassword = false
            };

            // Inactive on purpose: exercises the "Inactive-user rejection path" scenario without a
            // second organization needing one.
            var norteOperator = new User
            {
                FullName = "Norte Operator (inactive)",
                Email = "operator@seed-norte.test",
                PasswordHash = passwordHash,
                Organization = orgNorte,
                Profile = norteOperatorProfile,
                DocumentNumber = "30111112",
                IsActive = false,
                MustChangePassword = false
            };

            var surAdmin = new User
            {
                FullName = "Sur Administrator",
                Email = "admin@seed-sur.test",
                PasswordHash = passwordHash,
                Organization = orgSur,
                Profile = surAdminProfile,
                DocumentNumber = "30222222",
                MustChangePassword = false
            };

            return new SeedGraph(
                Roles: new List<Role> { administratorRole, operatorRole },
                Profiles: new List<Profile> { globalProfile, norteAdminProfile, norteOperatorProfile, surAdminProfile },
                Organizations: new List<Organization> { orgNorte, orgSur },
                Users: new List<User> { norteAdmin, norteOperator, surAdmin });
        }
    }
}
