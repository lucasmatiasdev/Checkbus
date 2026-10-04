using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Documents;
using Checkbus.ApiService.Domain.Entities.Rutas;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.ApiService.Infrastructure.Persistence
{
    public class CheckbusDbContext : DbContext
    {
        public CheckbusDbContext(DbContextOptions<CheckbusDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Organization> Organizations => Set<Organization>();
        public DbSet<DriverRequirement> DriverRequirements => Set<DriverRequirement>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<VehicleDocument> VehicleDocuments => Set<VehicleDocument>();
        public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();
        public DbSet<VehicleDiagnostic> VehicleDiagnostics => Set<VehicleDiagnostic>();
        public DbSet<ComponentDiagnostic> ComponentDiagnostics => Set<ComponentDiagnostic>();
        public DbSet<Ubicacion> Ubicaciones => Set<Ubicacion>();
        public DbSet<Evento> Eventos => Set<Evento>();
        public DbSet<Viaje> Viajes => Set<Viaje>();
        public DbSet<Ruta> Rutas => Set<Ruta>();
        public DbSet<Stop> Stops => Set<Stop>();

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

            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasKey(v => v.Id);
                entity.HasIndex(v => v.Patent).IsUnique();
                entity.Property(v => v.Status).HasConversion<string>();
                entity.Property(v => v.OwnerType).HasConversion<string>();
                entity.HasOne(v => v.Organization)
                    .WithMany()
                    .HasForeignKey(v => v.OrganizationId);
            });

            modelBuilder.Entity<VehicleDocument>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.VehicleId, x.Type }).IsUnique();
                entity.Property(x => x.Type).HasConversion<string>();
                entity.Property(x => x.Status).HasConversion<string>();
                entity.HasOne<Vehicle>()
                    .WithMany()
                    .HasForeignKey(x => x.VehicleId);
            });

            modelBuilder.Entity<MaintenanceRecord>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Type).HasConversion<string>();
                entity.Property(x => x.Status).HasConversion<string>();
                entity.HasOne<Vehicle>()
                    .WithMany()
                    .HasForeignKey(x => x.VehicleId);
            });

            modelBuilder.Entity<VehicleDiagnostic>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasOne<Vehicle>()
                    .WithMany()
                    .HasForeignKey(x => x.VehicleId);
                entity.HasOne<MaintenanceRecord>()
                    .WithMany()
                    .HasForeignKey(x => x.MaintenanceRecordId);
            });

            modelBuilder.Entity<ComponentDiagnostic>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Component).HasConversion<string>();
                entity.Property(x => x.Condition).HasConversion<string>();
                entity.HasOne<VehicleDiagnostic>()
                    .WithMany()
                    .HasForeignKey(x => x.VehicleDiagnosticId);
            });

            modelBuilder.Entity<Ubicacion>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.PlaceId).IsUnique();
            });

            modelBuilder.Entity<Evento>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Tipo).HasConversion<string>();
                entity.HasOne<Ubicacion>()
                    .WithMany()
                    .HasForeignKey(x => x.UbicacionId);
            });

            modelBuilder.Entity<Viaje>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Estado).HasConversion<string>();
                entity.HasOne(x => x.Organization)
                    .WithMany()
                    .HasForeignKey(x => x.OrganizationId);
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.ChoferId);
                entity.HasOne<Vehicle>()
                    .WithMany()
                    .HasForeignKey(x => x.VehicleId);
                entity.HasOne<Evento>()
                    .WithMany()
                    .HasForeignKey(x => x.EventoId);
            });

            modelBuilder.Entity<Ruta>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.ViajeId).IsUnique();
                entity.HasOne<Viaje>()
                    .WithMany()
                    .HasForeignKey(x => x.ViajeId);
            });

            modelBuilder.Entity<Stop>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Tipo).HasConversion<string>();
                entity.HasOne<Ruta>()
                    .WithMany()
                    .HasForeignKey(x => x.RutaId);
                entity.HasOne<Ubicacion>()
                    .WithMany()
                    .HasForeignKey(x => x.UbicacionId);
            });
        }
    }
}
