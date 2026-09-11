using Checkbus.Domain.Entities.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Checkbus.Infrastructure.Context.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);

            builder.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(256);

            builder.HasIndex(u => u.Email)
                .IsUnique();

            builder.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(u => u.DocumentNumber)
                .IsRequired()
                .HasMaxLength(20);

            builder.HasOne(u => u.Organization)
                .WithMany()
                .HasForeignKey("OrganizationId")
                .IsRequired();

            builder.HasOne(u => u.Profile)
                .WithMany()
                .HasForeignKey("ProfileId")
                .IsRequired();
        }
    }
}
