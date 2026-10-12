using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Payments;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Entities.Seats;
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
/// Proves <see cref="ReservationRepository"/>'s Fase D additions (odd/tasks/pagos-mercadopago.md):
/// CreateBatchAsync stamps a PaymentReservationLink row per Reservation (the mechanism
/// GetByPaymentIdAsync is built on, since Reservation itself carries no PaymentId FK — see
/// PaymentReservationLink's own doc comment), GetByPaymentIdAsync resolves them back regardless
/// of Status, and ConfirmAsync flips Status to Confirmada.
/// </summary>
public class ReservationRepositoryPaymentLinkTests
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

    private static async Task<(Trip Trip, Seat Seat1, Seat Seat2)> SeedTripWithTwoSeatsAsync(
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
            PlaceId = $"ChIJ-link-test-{Guid.NewGuid():N}",
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
            DepartureDate = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
            ArrivalDate = new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc),
            Capacity = 45,
            AvailableSeats = 45,
            Price = 1500m,
            Status = TripStatus.Publicado
        };
        db.Trips.Add(trip);

        var seat1 = new Seat { Id = Guid.NewGuid(), VehicleId = vehicle.Id, Number = 1 };
        var seat2 = new Seat { Id = Guid.NewGuid(), VehicleId = vehicle.Id, Number = 2 };
        db.Seats.AddRange(seat1, seat2);
        await db.SaveChangesAsync(cancellationToken);

        return (trip, seat1, seat2);
    }

    private static Passenger CreatePassenger() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Maria",
        Surname = "Gomez",
        DocumentType = DocumentType.DNI,
        DocumentNumber = $"3022{Random.Shared.Next(1000, 9999)}",
        Email = $"passenger-{Guid.NewGuid():N}@checkbus.test",
        Phone = "+54 11 5555-5555"
    };

    [Fact]
    public async Task CreateBatchAsync_StampsPaymentReservationLink_ForEveryReservationInTheBatch()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var (trip, seat1, seat2) = await SeedTripWithTwoSeatsAsync(db, cancellationToken);
        var passenger1 = CreatePassenger();
        var passenger2 = CreatePassenger();
        var reservation1 = new Reservation { Id = Guid.NewGuid(), TripId = trip.Id, SeatId = seat1.Id, PassengerId = passenger1.Id, Price = trip.Price, Status = ReservationStatus.PendientePago };
        var reservation2 = new Reservation { Id = Guid.NewGuid(), TripId = trip.Id, SeatId = seat2.Id, PassengerId = passenger2.Id, Price = trip.Price, Status = ReservationStatus.PendientePago };
        var payment = new Payment { Id = Guid.NewGuid(), ExternalOrderId = "mp-order-link", TotalAmount = new Money(3000m), CreatedAt = DateTime.UtcNow };

        var repository = new ReservationRepository(db);
        await repository.CreateBatchAsync(payment, [passenger1, passenger2], [reservation1, reservation2], cancellationToken);

        var resolved = await repository.GetByPaymentIdAsync(payment.Id, cancellationToken);
        Assert.Equal(2, resolved.Count);
        Assert.Contains(resolved, r => r.Id == reservation1.Id);
        Assert.Contains(resolved, r => r.Id == reservation2.Id);
    }

    [Fact]
    public async Task GetByPaymentIdAsync_IncludesCanceladaReservations_UnlikeGetByTripIdAsync()
    {
        // The orphaned-approval detection in HandlePaymentWebhookCommandHandler depends on
        // seeing a Cancelada row for this Payment, not having it filtered out.
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var (trip, seat1, _) = await SeedTripWithTwoSeatsAsync(db, cancellationToken);
        var passenger = CreatePassenger();
        var reservation = new Reservation { Id = Guid.NewGuid(), TripId = trip.Id, SeatId = seat1.Id, PassengerId = passenger.Id, Price = trip.Price, Status = ReservationStatus.Cancelada };
        var payment = new Payment { Id = Guid.NewGuid(), TotalAmount = new Money(1500m), CreatedAt = DateTime.UtcNow };

        var repository = new ReservationRepository(db);
        await repository.CreateBatchAsync(payment, [passenger], [reservation], cancellationToken);

        var resolved = await repository.GetByPaymentIdAsync(payment.Id, cancellationToken);
        Assert.Single(resolved);
        Assert.Equal(ReservationStatus.Cancelada, resolved[0].Status);
    }

    [Fact]
    public async Task ConfirmAsync_FlipsStatusToConfirmada()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var (trip, seat1, _) = await SeedTripWithTwoSeatsAsync(db, cancellationToken);
        var passenger = CreatePassenger();
        var reservation = new Reservation { Id = Guid.NewGuid(), TripId = trip.Id, SeatId = seat1.Id, PassengerId = passenger.Id, Price = trip.Price, Status = ReservationStatus.PendientePago };
        var payment = new Payment { Id = Guid.NewGuid(), TotalAmount = new Money(1500m), CreatedAt = DateTime.UtcNow };

        var repository = new ReservationRepository(db);
        await repository.CreateBatchAsync(payment, [passenger], [reservation], cancellationToken);

        await repository.ConfirmAsync(reservation.Id, cancellationToken);

        var reloaded = await repository.GetByIdAsync(reservation.Id, cancellationToken);
        Assert.NotNull(reloaded);
        Assert.Equal(ReservationStatus.Confirmada, reloaded!.Status);
    }
}
