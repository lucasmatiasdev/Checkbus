using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Documents;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Persistence
{
    public class CheckbusDbContext : DbContext
    {
        public CheckbusDbContext(DbContextOptions<CheckbusDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Organization> Organizations => Set<Organization>();
        public DbSet<DriverRequirement> DriverRequirements => Set<DriverRequirement>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => new { u.OrganizationId, u.DocumentNumber }).IsUnique();
                entity.Property(u => u.Name).HasMaxLength(100);
                entity.Property(u => u.Surname).HasMaxLength(100);
                entity.Property(u => u.Role).HasConversion<string>();
                entity.HasOne(u => u.Organization)
                    .WithMany()
                    .HasForeignKey(u => u.OrganizationId);
            });

            modelBuilder.Entity<Organization>(entity =>
            {
                entity.HasKey(o => o.Id);
                entity.HasIndex(o => o.Slug).IsUnique();
            });

            modelBuilder.Entity<DriverRequirement>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.UserId, x.Type }).IsUnique();
                entity.Property(x => x.Type).HasConversion<string>();
                entity.Property(x => x.Status).HasConversion<string>();
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId);
            });
        }
    }
}
