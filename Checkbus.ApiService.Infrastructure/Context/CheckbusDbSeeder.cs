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
    /// Placeholder only — real seed data content is out of scope for this change.
    /// </summary>
    public static class CheckbusDbSeeder
    {
        public static void Seed(DbContext context, bool isPopulated)
        {
            var db = (CheckbusDbContext)context;

            if (isPopulated || db.Users.Any(u => u.Email == "admin@checkbus.dev")) return;

            var organization = new Organization { Id = Guid.NewGuid(), CUIT = "20-12345678-9", Name = "Checkbus Demo", Slug = "checkbus-demo", LogoUrl = "", IsActive = true };
            var role = new Role { Id = Guid.NewGuid(), Name = "Admin" };

            var hasher = new IdentityPasswordHasher();
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                Email = "admin@checkbus.dev",
                PasswordHash = string.Empty,
                DocumentNumber = "12345678",
                RoleId = role.Id,
                Role = role,
                OrganizationId = organization.Id,
                Organization = organization,
                IsActive = true
            };
            user.PasswordHash = hasher.Hash(user, "Admin123!");

            db.Organizations.Add(organization);
            db.Roles.Add(role);
            db.Users.Add(user);
            db.SaveChanges();
        }

        public static Task SeedAsync(DbContext context, bool isPopulated, CancellationToken cancellationToken)
        {        
            var db = (CheckbusDbContext)context;

            if (isPopulated || db.Users.Any(u => u.Email == "admin@checkbus.dev")) return Task.CompletedTask;

            var organization = new Organization { Id = Guid.NewGuid(), CUIT = "20-12345678-9", Name = "Checkbus Demo", Slug = "checkbus-demo", LogoUrl = "", IsActive = true };
            var role = new Role { Id = Guid.NewGuid(), Name = "Admin" };

            var hasher = new IdentityPasswordHasher();
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                Email = "admin@checkbus.dev",
                PasswordHash = string.Empty,
                DocumentNumber = "12345678",
                RoleId = role.Id,
                Role = role,
                OrganizationId = organization.Id,
                Organization = organization,
                IsActive = true
            };
            user.PasswordHash = hasher.Hash(user, "Admin123!");

            db.Organizations.Add(organization);
            db.Roles.Add(role);
            db.Users.Add(user);
            return db.SaveChangesAsync();
        }
    }
}
