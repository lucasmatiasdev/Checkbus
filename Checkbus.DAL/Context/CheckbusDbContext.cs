using Checkbus.BEL.Auth;
using Checkbus.BEL.Fleet;
using Checkbus.DAL.Configurations;
using Microsoft.EntityFrameworkCore;
using Organization = Checkbus.BEL.Organization.Organization;

namespace Checkbus.DAL.Context;

/// <summary>
/// Application database context for Checkbus. Shared-schema multi-tenancy is
/// enforced through <see cref="ITenantProvider"/>: tenant-scoped entities get a
/// global query filter on <c>OrganizationId</c>, and entities implementing
/// <see cref="IOptionallyTenantScoped"/>-shaped semantics (e.g. <see cref="Role"/>)
/// get a disjunctive filter (tenant match OR global).
/// </summary>
public class CheckbusDbContext : DbContext
{
    private ITenantProvider? _tenantProvider;

    public CheckbusDbContext(DbContextOptions<CheckbusDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Optional tenant provider, set by the composition root after construction
    /// (property injection) so the design-time/migrations tooling only needs the
    /// single <see cref="DbContextOptions{TContext}"/> constructor.
    /// </summary>
    public void SetTenantProvider(ITenantProvider tenantProvider)
    {
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Current tenant, read live from <see cref="ITenantProvider"/> at query
    /// time. Referenced from the entity configuration classes' global query
    /// filters (via the <see cref="CheckbusDbContext"/> instance passed into
    /// their constructors) — EF Core re-binds instance-member accesses on the
    /// context to whichever instance is actually executing the query, not the
    /// (single, cached) instance that originally built the model.
    /// </summary>
    internal int? CurrentOrganizationId => _tenantProvider?.CurrentOrganizationId;

    /// <summary>
    /// When <c>true</c>, every tenant query filter is bypassed and the
    /// predefined-role immutability guard is bypassed (system/background jobs
    /// and explicit seeding).
    /// </summary>
    internal bool IsSystemMode => _tenantProvider?.IsSystemMode ?? false;

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Driver> Drivers => Set<Driver>();

    public DbSet<License> Licenses => Set<License>();

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<VehicleDocumentation> VehicleDocumentations => Set<VehicleDocumentation>();

    public DbSet<VehicleDiagnostic> VehicleDiagnostics => Set<VehicleDiagnostic>();

    public DbSet<ComponentDiagnostic> ComponentDiagnostics => Set<ComponentDiagnostic>();

    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();

    public DbSet<Attachment> Attachments => Set<Attachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new OrganizationConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration(this));
        modelBuilder.ApplyConfiguration(new RoleConfiguration(this));
        modelBuilder.ApplyConfiguration(new PermissionConfiguration());
        modelBuilder.ApplyConfiguration(new RolePermissionConfiguration(this));
        modelBuilder.ApplyConfiguration(new DriverConfiguration(this));
        modelBuilder.ApplyConfiguration(new LicenseConfiguration());
        modelBuilder.ApplyConfiguration(new VehicleConfiguration(this));
        modelBuilder.ApplyConfiguration(new VehicleDocumentationConfiguration());
        modelBuilder.ApplyConfiguration(new VehicleDiagnosticConfiguration());
        modelBuilder.ApplyConfiguration(new ComponentDiagnosticConfiguration());
        modelBuilder.ApplyConfiguration(new MaintenanceRecordConfiguration());
        modelBuilder.ApplyConfiguration(new AttachmentConfiguration(this));
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        GuardPredefinedRoleImmutability();
        GuardUserOrganizationImmutability();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        GuardPredefinedRoleImmutability();
        GuardUserOrganizationImmutability();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Blocks update/delete of predefined (global, <c>OrganizationId == null</c>)
    /// roles outside of an explicit system-mode seeding context.
    /// </summary>
    private void GuardPredefinedRoleImmutability()
    {
        if (IsSystemMode)
        {
            return;
        }

        var offending = ChangeTracker.Entries<Role>()
            .FirstOrDefault(e => e.Entity.OrganizationId == null
                && (e.State == EntityState.Modified || e.State == EntityState.Deleted));

        if (offending is not null)
        {
            throw new InvalidOperationException(
                $"Predefined role '{offending.Entity.Name}' (Id={offending.Entity.Id}) is global and cannot be modified or deleted outside system mode.");
        }
    }

    /// <summary>
    /// <see cref="User.OrganizationId"/> is immutable after insert — a user
    /// never migrates between tenants.
    /// </summary>
    private void GuardUserOrganizationImmutability()
    {
        foreach (var entry in ChangeTracker.Entries<User>())
        {
            if (entry.State != EntityState.Modified)
            {
                continue;
            }

            var organizationIdProperty = entry.Property(u => u.OrganizationId);
            if (organizationIdProperty.IsModified)
            {
                throw new InvalidOperationException(
                    $"User {entry.Entity.Id}'s OrganizationId is immutable after insert.");
            }
        }
    }
}
