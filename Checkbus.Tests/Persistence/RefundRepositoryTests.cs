using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
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
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Persistence;

/// <summary>
/// Proves <see cref="RefundRepository"/> (odd/tasks/pagos-mercadopago.md Fase B, B5): plain
/// CRUD round-trips plus <see cref="RefundRepository.GetByPaymentIdAsync"/>, which Fase F1 will
/// use to decide whether cancelling one seat is the last still-active one on its covering
/// Payment (full refund) or one of several (partial refund). <see cref="Refund"/> carries real
/// FKs to Payment/Reservation (FK-only, no navigation properties), so this needs a valid parent
/// graph, not floating Guids.
/// </summary>
public class RefundRepositoryTests
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

    private static async Task<(Payment Payment, Reservation ReservationA, Reservation ReservationB)> SeedPaymentWithTwoReservationsAsync(
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
            PlaceId = $"ChIJ-refund-test-{Guid.NewGuid():N}",
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

        var seatA = new Seat { Id = Guid.NewGuid(), VehicleId = vehicle.Id, Number = 1 };
        var seatB = new Seat { Id = Guid.NewGuid(), VehicleId = vehicle.Id, Number = 2 };
        db.Seats.AddRange(seatA, seatB);

        var passengerA = new Passenger
        {
            Id = Guid.NewGuid(),
            Name = "Maria",
            Surname = "Gomez",
            DocumentType = DocumentType.DNI,
            DocumentNumber = $"3022{Random.Shared.Next(1000, 9999)}",
            Email = $"passenger-a-{Guid.NewGuid():N}@checkbus.test",
            Phone = "+54 11 5555-5555"
        };
        var passengerB = new Passenger
        {
            Id = Guid.NewGuid(),
            Name = "Pedro",
            Surname = "Lopez",
            DocumentType = DocumentType.DNI,
            DocumentNumber = $"3023{Random.Shared.Next(1000, 9999)}",
            Email = $"passenger-b-{Guid.NewGuid():N}@checkbus.test",
            Phone = "+54 11 5555-5556"
        };
        db.Passengers.AddRange(passengerA, passengerB);
        await db.SaveChangesAsync(cancellationToken);

        var reservationA = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = seatA.Id,
            PassengerId = passengerA.Id,
            Price = trip.Price
        };
        var reservationB = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = seatB.Id,
            PassengerId = passengerB.Id,
            Price = trip.Price
        };
        db.Reservations.AddRange(reservationA, reservationB);
        await db.SaveChangesAsync(cancellationToken);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TotalAmount = new Money(trip.Price * 2),
            Status = PaymentStatus.Aprobado,
            CreatedAt = DateTime.UtcNow
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);

        return (payment, reservationA, reservationB);
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTrips()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var (payment, reservationA, _) = await SeedPaymentWithTwoReservationsAsync(db, cancellationToken);
        var repository = new RefundRepository(db);
        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            ReservationId = reservationA.Id,
            Amount = new Money(1500m),
            CreatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(refund, cancellationToken);
        var result = await repository.GetByIdAsync(refund.Id, cancellationToken);

        Assert.NotNull(result);
        Assert.Equal(payment.Id, result!.PaymentId);
        Assert.Equal(reservationA.Id, result.ReservationId);
        Assert.Equal(1500m, result.Amount.Amount);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new RefundRepository(db);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), cancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByPaymentIdAsync_ReturnsOnlyRefundsForThatPayment()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var (paymentA, reservationA, reservationB) = await SeedPaymentWithTwoReservationsAsync(db, cancellationToken);
        var (paymentB, reservationC, _) = await SeedPaymentWithTwoReservationsAsync(db, cancellationToken);

        var repository = new RefundRepository(db);
        await repository.AddAsync(new Refund
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentA.Id,
            ReservationId = reservationA.Id,
            Amount = new Money(1500m),
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
        await repository.AddAsync(new Refund
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentA.Id,
            ReservationId = reservationB.Id,
            Amount = new Money(1500m),
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
        await repository.AddAsync(new Refund
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentB.Id,
            ReservationId = reservationC.Id,
            Amount = new Money(1500m),
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        var result = await repository.GetByPaymentIdAsync(paymentA.Id, cancellationToken);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(paymentA.Id, r.PaymentId));
    }

    [Fact]
    public async Task GetByPaymentIdAsync_PaymentWithNoRefunds_ReturnsEmpty()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var (payment, _, _) = await SeedPaymentWithTwoReservationsAsync(db, cancellationToken);
        var repository = new RefundRepository(db);

        var result = await repository.GetByPaymentIdAsync(payment.Id, cancellationToken);

        Assert.Empty(result);
    }
}
