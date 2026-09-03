using Checkbus.BEL.Fleet;
using Checkbus.DAL.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Checkbus.DAL.Configurations;

/// <summary>
/// Polymorphic ownership: exactly one of <see cref="Attachment.VehicleDocumentationId"/>
/// or <see cref="Attachment.DriverId"/> must be set, enforced by a database
/// CHECK constraint (a <c>SupportTicket</c> owner column will be added,
/// together with a widened CHECK constraint, in the Semana 7 change). The
/// query filter is navigation-based through whichever owner is set, since
/// there is no direct <c>OrganizationId</c> column here.
/// </summary>
public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    private readonly CheckbusDbContext _context;

    public AttachmentConfiguration(CheckbusDbContext context)
    {
        _context = context;
    }

    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachments", t => t.HasCheckConstraint(
            "CK_Attachments_ExactlyOneOwner",
            "(\"VehicleDocumentationId\" IS NOT NULL AND \"DriverId\" IS NULL) " +
            "OR (\"VehicleDocumentationId\" IS NULL AND \"DriverId\" IS NOT NULL)"));

        builder.HasKey(a => a.Id);

        builder.Property(a => a.StoragePath).IsRequired().HasMaxLength(500);
        builder.Property(a => a.FileName).IsRequired().HasMaxLength(255);
        builder.Property(a => a.Format).IsRequired().HasMaxLength(10);
        builder.Property(a => a.UploadedAt).IsRequired().HasColumnType("timestamptz");

        builder.HasOne(a => a.VehicleDocumentation)
            .WithMany(vd => vd.Attachments)
            .HasForeignKey(a => a.VehicleDocumentationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Driver)
            .WithMany(d => d.Attachments)
            .HasForeignKey(a => a.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(a =>
            _context.IsSystemMode
            || (a.VehicleDocumentation != null && a.VehicleDocumentation.Vehicle.OrganizationId == _context.CurrentOrganizationId)
            || (a.Driver != null && a.Driver.OrganizationId == _context.CurrentOrganizationId));
    }
}
