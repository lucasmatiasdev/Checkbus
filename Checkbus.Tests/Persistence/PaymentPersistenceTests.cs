using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Invoices;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Payments;
using Checkbus.ApiService.Domain.Entities.Refunds;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Entities.Seats;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.ValueObjects;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Persistence;

/// <summary>
/// Fase A of pagos-mercadopago (odd/tasks/pagos-mercadopago.md) is domain + persistence only —
/// no repositories yet. These tests exercise <see cref="Payment"/>, <see cref="Invoice"/> and
/// <see cref="Refund"/> directly against <see cref="CheckbusDbContext"/>, same SQLite-in-memory
/// setup used by every other persistence test in this folder (e.g. ReservationPersistenceTests).
/// </summary>
public class PaymentPersistenceTests
{
    private static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=True");
        connection.Open();
        return connection;
    }

    private static CheckbusDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<CheckbusDbContext>()
            .UseSqlite(connection)
            .Options;
        return new CheckbusDbContext(options);
    }

    // Invoice/Refund carry real FKs to Trip/Passenger/Reservation (FK-only, no navigation
    // properties — same posture as every other entity here), so persistence tests for them
    // need a full, valid parent graph, not floating Guids. Mirrors the setup already used by
    // ReservationPersistenceTests.
    private static async Task<(Trip Trip, Passenger Passenger, Reservation Reservation)> SeedReservationGraphAsync(
        CheckbusDbContext db, CancellationToken cancellationToken)
    {
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            CUIT = "20-12345678-9",
            Name = "Checkbus Demo",
            Slug = $"checkbus-demo-{Guid.NewGuid():N}",
            LogoUrl = "",
            IsActive = true
        };
        db.Organizations.Add(organization);

        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            Brand = "Mercedes-Benz",
            Model = "O500",
            Year = 2020,
            Patent = $"AB{Random.Shared.Next(100, 999)}CD",
            Capacity = 45,
            Mileage = 1000,
            Status = VehicleStatus.Activo,
            OrganizationId = organization.Id,
            OwnerType = VehicleOwnerType.Organizacion
        };
        db.Vehicles.Add(vehicle);

        var driver = new User
        {
            Id = Guid.NewGuid(),
            Name = "Juan",
            Surname = "Perez",
            Email = $"driver-{Guid.NewGuid():N}@checkbus.test",
            PasswordHash = "hash",
            DocumentNumber = $"3011{Random.Shared.Next(1000, 9999)}",
            Role = Role.Chofer,
            OrganizationId = organization.Id
        };
        db.Users.Add(driver);

        var location = new Location
        {
            Id = Guid.NewGuid(),
            Name = "Estadio Monumental",
            Address = "Av. Pres. Figueroa Alcorta 7597",
            PlaceId = $"ChIJ-payment-test-{Guid.NewGuid():N}",
            Latitude = -34.5453,
            Longitude = -58.4498
        };
        db.Locations.Add(location);
        await db.SaveChangesAsync(cancellationToken);

        var @event = new Event
        {
            Id = Guid.NewGuid(),
            Name = "River vs Boca",
            Type = EventType.Partido,
            Date = new DateTime(2026, 11, 1, 20, 0, 0, DateTimeKind.Utc),
            LocationId = location.Id,
            OrganizationId = organization.Id
        };
        db.Events.Add(@event);
        await db.SaveChangesAsync(cancellationToken);

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            DriverId = driver.Id,
            VehicleId = vehicle.Id,
            EventId = @event.Id,
            Slug = Guid.NewGuid().ToString(),
            DepartureDate = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
            ArrivalDate = new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc),
            Capacity = 45,
            AvailableSeats = 45,
            Price = 1500m,
            Status = TripStatus.Publicado
        };
        db.Trips.Add(trip);

        var seat = new Seat { Id = Guid.NewGuid(), VehicleId = vehicle.Id, Number = 1 };
        db.Seats.Add(seat);

        var passenger = new Passenger
        {
            Id = Guid.NewGuid(),
            Name = "Maria",
            Surname = "Gomez",
            DocumentType = DocumentType.DNI,
            DocumentNumber = $"3022{Random.Shared.Next(1000, 9999)}",
            Email = $"passenger-{Guid.NewGuid():N}@checkbus.test",
            Phone = "+54 11 5555-5555"
        };
        db.Passengers.Add(passenger);
        await db.SaveChangesAsync(cancellationToken);

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = seat.Id,
            PassengerId = passenger.Id,
            Price = trip.Price
        };
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync(cancellationToken);

        return (trip, passenger, reservation);
    }

    [Fact]
    public async Task Payment_PersistsAndRoundTrips_IncludingOwnedMoney()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ExternalOrderId = null,
            TotalAmount = new Money(4500m, "ARS"),
            CreatedAt = new DateTime(2026, 10, 12, 10, 0, 0, DateTimeKind.Utc)
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);

        var reloaded = await db.Payments.FirstAsync(p => p.Id == payment.Id, cancellationToken);
        Assert.Equal(PaymentStatus.Pendiente, reloaded.Status);
        Assert.Null(reloaded.ExternalOrderId);
        Assert.Equal(4500m, reloaded.TotalAmount.Amount);
        Assert.Equal("ARS", reloaded.TotalAmount.Currency);
    }

    [Fact]
    public async Task Payment_StatusAndExternalOrderId_RoundTripAfterUpdate()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TotalAmount = new Money(1500m),
            CreatedAt = DateTime.UtcNow
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);

        payment.ExternalOrderId = "mp-order-123";
        payment.Status = PaymentStatus.Aprobado;
        await db.SaveChangesAsync(cancellationToken);

        var reloaded = await db.Payments.FirstAsync(p => p.Id == payment.Id, cancellationToken);
        Assert.Equal("mp-order-123", reloaded.ExternalOrderId);
        Assert.Equal(PaymentStatus.Aprobado, reloaded.Status);
    }

    [Fact]
    public async Task Invoice_PersistsAndRoundTrips()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var (trip, passenger, _) = await SeedReservationGraphAsync(db, cancellationToken);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TotalAmount = new Money(1500m),
            Status = PaymentStatus.Aprobado,
            CreatedAt = DateTime.UtcNow
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            PassengerId = passenger.Id,
            PaymentId = payment.Id,
            IssuedAt = new DateTime(2026, 10, 12, 11, 0, 0, DateTimeKind.Utc)
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(cancellationToken);

        var reloaded = await db.Invoices.FirstAsync(i => i.Id == invoice.Id, cancellationToken);
        Assert.Equal(trip.Id, reloaded.TripId);
        Assert.Equal(passenger.Id, reloaded.PassengerId);
        Assert.Equal(payment.Id, reloaded.PaymentId);
        Assert.Equal(invoice.IssuedAt, reloaded.IssuedAt);
    }

    [Fact]
    public async Task Refund_PersistsAndRoundTrips_IncludingOwnedMoneyAndReservationReference()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var (_, _, reservation) = await SeedReservationGraphAsync(db, cancellationToken);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TotalAmount = new Money(4500m),
            Status = PaymentStatus.ParcialmenteReembolsado,
            CreatedAt = DateTime.UtcNow
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);

        // Refund references the specific Reservation it covers, not just the Payment — this is
        // a deliberate design decision (see Refund's doc comment): a partial refund of 1-of-3
        // seats must be traceable to which seat's price it refunded.
        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            ReservationId = reservation.Id,
            Amount = new Money(1500m, "ARS"),
            ExternalRefundId = "mp-refund-456",
            CreatedAt = new DateTime(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc)
        };
        db.Refunds.Add(refund);
        await db.SaveChangesAsync(cancellationToken);

        var reloaded = await db.Refunds.FirstAsync(r => r.Id == refund.Id, cancellationToken);
        Assert.Equal(payment.Id, reloaded.PaymentId);
        Assert.Equal(reservation.Id, reloaded.ReservationId);
        Assert.Equal(1500m, reloaded.Amount.Amount);
        Assert.Equal("ARS", reloaded.Amount.Currency);
        Assert.Equal("mp-refund-456", reloaded.ExternalRefundId);
    }

    [Fact]
    public async Task Refund_ExternalRefundId_CanBeNull()
    {
        // ExternalRefundId is nullable until the Mercado Pago refund call actually succeeds
        // (Fase B/F) — mirrors Payment.ExternalOrderId's same nullable-until-confirmed shape.
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var (_, _, reservation) = await SeedReservationGraphAsync(db, cancellationToken);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TotalAmount = new Money(1500m),
            CreatedAt = DateTime.UtcNow
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);

        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            ReservationId = reservation.Id,
            Amount = new Money(1500m),
            ExternalRefundId = null,
            CreatedAt = DateTime.UtcNow
        };
        db.Refunds.Add(refund);
        await db.SaveChangesAsync(cancellationToken);

        var reloaded = await db.Refunds.FirstAsync(r => r.Id == refund.Id, cancellationToken);
        Assert.Null(reloaded.ExternalRefundId);
    }
}
