using Checkbus.BEL.Fleet;
using Checkbus.BEL.Fleet.Enums;
using Microsoft.EntityFrameworkCore;
using Organization = Checkbus.BEL.Organization.Organization;

namespace Checkbus.Tests.Integration;

/// <summary>
/// S3.4 — Fleet entities: unique indexes (plate, DNI) scoped per organization,
/// and the Attachment polymorphic-ownership CHECK constraint ("exactly one
/// owner").
/// </summary>
public class FleetEntitiesTests
{
    [Fact]
    public void Duplicate_Plate_Within_Same_Organization_Throws()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();

        int orgId;
        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();

            var org = new Organization { Name = "Org A", Cuit = "30-20202020-2" };
            systemContext.Organizations.Add(org);
            systemContext.SaveChanges();
            orgId = org.Id;

            systemContext.Vehicles.Add(new Vehicle { OrganizationId = orgId, Plate = "AB123CD", Capacity = 40 });
            systemContext.SaveChanges();

            systemContext.Vehicles.Add(new Vehicle { OrganizationId = orgId, Plate = "AB123CD", Capacity = 40 });

            Assert.Throws<DbUpdateException>(() => systemContext.SaveChanges());
        }
    }

    [Fact]
    public void Same_Plate_Is_Allowed_Across_Different_Organizations()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();

        using (var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true }))
        {
            systemContext.Database.EnsureCreated();

            var orgA = new Organization { Name = "Org A", Cuit = "30-21212121-2" };
            var orgB = new Organization { Name = "Org B", Cuit = "30-22222222-2" };
            systemContext.Organizations.AddRange(orgA, orgB);
            systemContext.SaveChanges();

            systemContext.Vehicles.Add(new Vehicle { OrganizationId = orgA.Id, Plate = "XY999ZZ", Capacity = 20 });
            systemContext.Vehicles.Add(new Vehicle { OrganizationId = orgB.Id, Plate = "XY999ZZ", Capacity = 20 });

            // Must not throw: the unique index is scoped per-organization.
            systemContext.SaveChanges();
        }

        using var verifyContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true });
        Assert.Equal(2, verifyContext.Vehicles.Count(v => v.Plate == "XY999ZZ"));
    }

    [Fact]
    public void Duplicate_Dni_Within_Same_Organization_Throws()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();

        using var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true });
        systemContext.Database.EnsureCreated();

        var org = new Organization { Name = "Org A", Cuit = "30-23232323-2" };
        systemContext.Organizations.Add(org);
        systemContext.SaveChanges();

        systemContext.Drivers.Add(new Driver { OrganizationId = org.Id, FullName = "Juan Perez", Dni = "30111222" });
        systemContext.SaveChanges();

        systemContext.Drivers.Add(new Driver { OrganizationId = org.Id, FullName = "Otro Chofer", Dni = "30111222" });

        Assert.Throws<DbUpdateException>(() => systemContext.SaveChanges());
    }

    [Fact]
    public void Attachment_With_Neither_Owner_Set_Throws()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();

        using var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true });
        systemContext.Database.EnsureCreated();

        systemContext.Attachments.Add(new Attachment
        {
            StoragePath = "1/misc/file.pdf",
            FileName = "file.pdf",
            Format = "pdf",
            SizeBytes = 1024,
            UploadedAt = DateTime.UtcNow,
        });

        Assert.Throws<DbUpdateException>(() => systemContext.SaveChanges());
    }

    [Fact]
    public void Attachment_With_Both_Owners_Set_Throws()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();

        using var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true });
        systemContext.Database.EnsureCreated();

        var org = new Organization { Name = "Org A", Cuit = "30-24242424-2" };
        systemContext.Organizations.Add(org);
        systemContext.SaveChanges();

        var driver = new Driver { OrganizationId = org.Id, FullName = "Juan Perez", Dni = "30333444" };
        var vehicle = new Vehicle { OrganizationId = org.Id, Plate = "CC111DD", Capacity = 30 };
        systemContext.Drivers.Add(driver);
        systemContext.Vehicles.Add(vehicle);
        systemContext.SaveChanges();

        var documentation = new VehicleDocumentation
        {
            VehicleId = vehicle.Id,
            Type = VehicleDocumentationType.Seguro,
            DocumentNumber = "DOC-1",
            State = VehicleDocumentationState.Vigente,
        };
        systemContext.VehicleDocumentations.Add(documentation);
        systemContext.SaveChanges();

        systemContext.Attachments.Add(new Attachment
        {
            DriverId = driver.Id,
            VehicleDocumentationId = documentation.Id,
            StoragePath = "1/misc/file.pdf",
            FileName = "file.pdf",
            Format = "pdf",
            SizeBytes = 1024,
            UploadedAt = DateTime.UtcNow,
        });

        Assert.Throws<DbUpdateException>(() => systemContext.SaveChanges());
    }

    [Fact]
    public void Attachment_With_Exactly_One_Owner_Set_Succeeds()
    {
        using var connection = SqliteDbContextTestHelper.OpenConnection();

        using var systemContext = SqliteDbContextTestHelper.CreateContext(
            connection, new FakeTenantProvider { IsSystemMode = true });
        systemContext.Database.EnsureCreated();

        var org = new Organization { Name = "Org A", Cuit = "30-25252525-2" };
        systemContext.Organizations.Add(org);
        systemContext.SaveChanges();

        var driver = new Driver { OrganizationId = org.Id, FullName = "Juan Perez", Dni = "30555666" };
        systemContext.Drivers.Add(driver);
        systemContext.SaveChanges();

        systemContext.Attachments.Add(new Attachment
        {
            DriverId = driver.Id,
            StoragePath = $"{org.Id}/drivers/{driver.Id}/license.pdf",
            FileName = "license.pdf",
            Format = "pdf",
            SizeBytes = 2048,
            UploadedAt = DateTime.UtcNow,
        });

        // Must not throw: exactly one owner is set.
        systemContext.SaveChanges();

        Assert.Single(systemContext.Attachments.Where(a => a.DriverId == driver.Id));
    }
}
