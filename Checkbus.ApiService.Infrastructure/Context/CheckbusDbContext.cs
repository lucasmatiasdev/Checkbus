using Checkbus.ApiService.Domain.Entities.Audit;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.BoardingRecords;
using Checkbus.ApiService.Domain.Entities.Documents;
using Checkbus.ApiService.Domain.Entities.Invoices;
using Checkbus.ApiService.Domain.Entities.Notifications;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Payments;
using Checkbus.ApiService.Domain.Entities.Refunds;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Entities.Seats;
using Checkbus.ApiService.Domain.Entities.Support;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Infrastructure.Implementations.Payments;
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
        public DbSet<Location> Locations => Set<Location>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<Trip> Trips => Set<Trip>();
        public DbSet<Route> Routes => Set<Route>();
        public DbSet<Stop> Stops => Set<Stop>();
        public DbSet<Seat> Seats => Set<Seat>();
        public DbSet<Passenger> Passengers => Set<Passenger>();
        public DbSet<Reservation> Reservations => Set<Reservation>();
        public DbSet<BoardingRecord> BoardingRecords => Set<BoardingRecord>();
        public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<Refund> Refunds => Set<Refund>();

        public DbSet<Notification> Notifications => Set<Notification>();

        public DbSet<MercadoPagoConnection> MercadoPagoConnections => Set<MercadoPagoConnection>();

        public DbSet<ProcessedWebhookEvent> ProcessedWebhookEvents => Set<ProcessedWebhookEvent>();

        public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

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
                entity.HasIndex(v => new { v.OrganizationId, v.Patent }).IsUnique();
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

            modelBuilder.Entity<Location>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.PlaceId).IsUnique();
            });

            modelBuilder.Entity<Event>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Type).HasConversion<string>();
                entity.HasOne(x => x.Organization)
                    .WithMany()
                    .HasForeignKey(x => x.OrganizationId);
                entity.HasOne<Location>()
                    .WithMany()
                    .HasForeignKey(x => x.LocationId);
            });

            modelBuilder.Entity<Trip>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Status).HasConversion<string>();
                entity.HasOne(x => x.Organization)
                    .WithMany()
                    .HasForeignKey(x => x.OrganizationId);
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.DriverId);
                entity.HasOne<Vehicle>()
                    .WithMany()
                    .HasForeignKey(x => x.VehicleId);
                entity.HasOne<Event>()
                    .WithMany()
                    .HasForeignKey(x => x.EventId);
                entity.HasIndex(x => new { x.OrganizationId, x.Slug }).IsUnique();
            });

            modelBuilder.Entity<Route>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.TripId).IsUnique();
                entity.HasOne<Trip>()
                    .WithMany()
                    .HasForeignKey(x => x.TripId);
            });

            modelBuilder.Entity<Stop>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Type).HasConversion<string>();
                entity.HasOne<Route>()
                    .WithMany()
                    .HasForeignKey(x => x.RouteId);
                entity.HasOne<Location>()
                    .WithMany()
                    .HasForeignKey(x => x.LocationId);
            });

            modelBuilder.Entity<Seat>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.VehicleId, x.Number }).IsUnique();
                entity.HasOne<Vehicle>()
                    .WithMany()
                    .HasForeignKey(x => x.VehicleId);
            });

            modelBuilder.Entity<Passenger>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.DocumentType).HasConversion<string>();
            });

            modelBuilder.Entity<Reservation>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Status).HasConversion<string>();

                entity.HasIndex(x => new { x.TripId, x.SeatId })
                    .IsUnique()
                    .HasFilter("\"Status\" <> 'Cancelada'");
                entity.HasOne<Trip>()
                    .WithMany()
                    .HasForeignKey(x => x.TripId);
                entity.HasOne<Seat>()
                    .WithMany()
                    .HasForeignKey(x => x.SeatId);
                entity.HasOne<Passenger>()
                    .WithMany()
                    .HasForeignKey(x => x.PassengerId);

                entity.HasOne<Payment>()
                    .WithMany()
                    .HasForeignKey(x => x.PaymentId);
            });

            modelBuilder.Entity<BoardingRecord>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Status).HasConversion<string>();
                entity.Property(x => x.Method).HasConversion<string>();

                entity.HasIndex(x => x.ReservationId).IsUnique();
                entity.HasOne<Reservation>()
                    .WithMany()
                    .HasForeignKey(x => x.ReservationId);
            });

            modelBuilder.Entity<SupportTicket>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Category).HasConversion<string>();
                entity.Property(x => x.Status).HasConversion<string>();
                entity.Property(x => x.Subject).HasMaxLength(200);
                entity.HasOne<Organization>()
                    .WithMany()
                    .HasForeignKey(x => x.OrganizationId);
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.CreatedByUserId);
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Status).HasConversion<string>();

                entity.OwnsOne(x => x.TotalAmount, money =>
                {
                    money.Property(m => m.Amount).HasColumnName("TotalAmount");
                    money.Property(m => m.Currency).HasColumnName("TotalAmountCurrency");
                });
                entity.Navigation(x => x.TotalAmount).IsRequired();
            });

            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasOne<Trip>()
                    .WithMany()
                    .HasForeignKey(x => x.TripId);
                entity.HasOne<Passenger>()
                    .WithMany()
                    .HasForeignKey(x => x.PassengerId);
                entity.HasOne<Payment>()
                    .WithMany()
                    .HasForeignKey(x => x.PaymentId);
            });

            modelBuilder.Entity<Refund>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.OwnsOne(x => x.Amount, money =>
                {
                    money.Property(m => m.Amount).HasColumnName("Amount");
                    money.Property(m => m.Currency).HasColumnName("AmountCurrency");
                });
                entity.Navigation(x => x.Amount).IsRequired();
                entity.HasOne<Payment>()
                    .WithMany()
                    .HasForeignKey(x => x.PaymentId);

                entity.HasOne<Reservation>()
                    .WithMany()
                    .HasForeignKey(x => x.ReservationId);
            });

            modelBuilder.Entity<ProcessedWebhookEvent>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.ExternalEventId).IsUnique();
            });

            modelBuilder.Entity<MercadoPagoConnection>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.OrganizationId).IsUnique();
                entity.HasOne<Organization>()
                    .WithMany()
                    .HasForeignKey(x => x.OrganizationId);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.TargetType).HasConversion<string>();
                entity.Property(x => x.Reason).HasConversion<string>();

                entity.HasIndex(x => new { x.TargetType, x.TargetId });
                entity.HasOne<Organization>()
                    .WithMany()
                    .HasForeignKey(x => x.OrganizationId);
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.CreatedByUserId);
            });

            modelBuilder.Entity<AuditLogEntry>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Action).HasConversion<string>();

                entity.HasOne<Organization>()
                    .WithMany()
                    .HasForeignKey(x => x.OrganizationId)
                    .IsRequired(false);
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.ActorUserId)
                    .IsRequired(false);

                entity.HasIndex(x => x.CreatedAt);
            });
        }
    }
}
