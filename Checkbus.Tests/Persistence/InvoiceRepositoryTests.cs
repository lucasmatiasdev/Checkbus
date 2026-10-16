using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Invoices;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Payments;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.ValueObjects;
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Persistence;

/// <summary>
/// Proves <see cref="InvoiceRepository"/> (odd/tasks/pagos-mercadopago.md Fase B, B5): plain
/// CRUD round-trips. <see cref="Invoice"/> carries real FKs to Trip/Passenger/Payment (FK-only,
/// no navigation properties), so this needs a valid parent graph, not floating Guids — same
/// setup PaymentPersistenceTests already uses for its own Invoice coverage.
/// </summary>
public class InvoiceRepositoryTests
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

    private static async Task<(Trip Trip, Passenger Passenger)> SeedTripAndPassengerAsync(
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
            PlaceId = $"ChIJ-invoice-test-{Guid.NewGuid():N}",
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

        return (trip, passenger);
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTrips()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var (trip, passenger) = await SeedTripAndPassengerAsync(db, cancellationToken);
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TotalAmount = new Money(1500m),
            CreatedAt = DateTime.UtcNow
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new InvoiceRepository(db);
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            PassengerId = passenger.Id,
            PaymentId = payment.Id,
            IssuedAt = DateTime.UtcNow
        };

        await repository.AddAsync(invoice, cancellationToken);
        var result = await repository.GetByIdAsync(invoice.Id, cancellationToken);

        Assert.NotNull(result);
        Assert.Equal(trip.Id, result!.TripId);
        Assert.Equal(passenger.Id, result.PassengerId);
        Assert.Equal(payment.Id, result.PaymentId);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new InvoiceRepository(db);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), cancellationToken);

        Assert.Null(result);
    }
}
