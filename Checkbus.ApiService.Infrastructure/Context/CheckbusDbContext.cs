using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.BoardingRecords;
using Checkbus.ApiService.Domain.Entities.Documents;
using Checkbus.ApiService.Domain.Entities.Invoices;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Payments;
using Checkbus.ApiService.Domain.Entities.Refunds;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Entities.Seats;
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
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<Refund> Refunds => Set<Refund>();

        // Fase I (odd/tasks/pagos-mercadopago.md) — Marketplace/Split Payments: one connected
        // Mercado Pago seller account per Organization.
        public DbSet<MercadoPagoConnection> MercadoPagoConnections => Set<MercadoPagoConnection>();

        // Fase D (odd/tasks/pagos-mercadopago.md) — Infrastructure-only EF-mapped class with no
        // Domain/business meaning, see its own doc comment for why.
        public DbSet<ProcessedWebhookEvent> ProcessedWebhookEvents => Set<ProcessedWebhookEvent>();

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
                // Real double-booking guard (odd/tasks/reservas-publicas.md R4): one Seat can
                // have at most one ACTIVE Reservation per Trip. Filtered as of Fase 4
                // (cancellation): a Cancelada row must stop counting against this guard so the
                // freed Seat can be re-booked by someone else — the raw SQL filter string is
                // evaluated by the database (Npgsql in production, SQLite in tests), matching
                // the Status column's own HasConversion<string>() representation
                // ("Confirmada"/"Cancelada"), not the enum's numeric value. This is the actual
                // correctness guarantee against the TOCTOU race; the handler's pre-check is
                // just a nicer error path for the common non-racing case.
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
                // D7 (odd/tasks/pagos-mercadopago.md): plain 1:N, one Reservation belongs to
                // exactly one Payment — replaces the PaymentReservationLink join table. Nullable
                // FK, no navigation property, same posture as TripId/SeatId/PassengerId above.
                entity.HasOne<Payment>()
                    .WithMany()
                    .HasForeignKey(x => x.PaymentId);
            });

            modelBuilder.Entity<BoardingRecord>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Status).HasConversion<string>();
                entity.Property(x => x.Method).HasConversion<string>();
                // True 1:1 with Reservation (odd/tasks/reservas-publicas.md Fase 3 / A1).
                entity.HasIndex(x => x.ReservationId).IsUnique();
                entity.HasOne<Reservation>()
                    .WithMany()
                    .HasForeignKey(x => x.ReservationId);
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Status).HasConversion<string>();
                // Money is a value object, not its own table — mapped inline as an EF Core
                // owned type (odd/tasks/pagos-mercadopago.md A4/A6). IsRequired() because
                // Payment.TotalAmount is a non-nullable `required` property.
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
                // FK-only, no navigation properties, same pattern as every other entity here —
                // Trip/Passenger/Payment are related tables but this entity never dereferences
                // them in code (odd/tasks/pagos-mercadopago.md A3).
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
                // Deliberate: references the specific Reservation this refund covers, not just
                // the Payment — see Refund's own doc comment for why (partial-refund traceability).
                entity.HasOne<Reservation>()
                    .WithMany()
                    .HasForeignKey(x => x.ReservationId);
            });

            modelBuilder.Entity<ProcessedWebhookEvent>(entity =>
            {
                entity.HasKey(x => x.Id);
                // The whole point of this table: a given Mercado Pago notification id is
                // recorded at most once (Fase D3's dedupe guarantee).
                entity.HasIndex(x => x.ExternalEventId).IsUnique();
            });

            modelBuilder.Entity<MercadoPagoConnection>(entity =>
            {
                entity.HasKey(x => x.Id);
                // At most one connection per Organization (odd/tasks/pagos-mercadopago.md Fase I,
                // I1) — the DB itself enforces this, not just the repository's own
                // find-then-insert-or-update UpsertAsync logic.
                entity.HasIndex(x => x.OrganizationId).IsUnique();
                entity.HasOne<Organization>()
                    .WithMany()
                    .HasForeignKey(x => x.OrganizationId);
            });
        }
    }
}
