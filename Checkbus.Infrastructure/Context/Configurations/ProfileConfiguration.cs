using Checkbus.Domain.Entities.Authentication.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Checkbus.Infrastructure.Context.Configurations
{
    public class ProfileConfiguration : IEntityTypeConfiguration<Profile>
    {
        public void Configure(EntityTypeBuilder<Profile> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasOne(p => p.Organization)
                .WithMany()
                .HasForeignKey("OrganizationId")
                .IsRequired(false);

            // A6: explicit Profile<->Role many-to-many, unidirectional (no reverse nav on Role).
            builder.HasMany(p => p.Roles)
                .WithMany()
                .UsingEntity(
                    "profile_role",
                    r => r.HasOne(typeof(Role)).WithMany().HasForeignKey("role_id"),
                    l => l.HasOne(typeof(Profile)).WithMany().HasForeignKey("profile_id"),
                    j => j.HasKey("profile_id", "role_id"));
        }
    }
}
