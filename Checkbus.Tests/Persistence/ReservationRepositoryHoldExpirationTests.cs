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
/// Proves <see cref="ReservationRepository.ReleaseExpiredHoldsAsync"/> (odd/tasks/pagos-mercadopago.md
/// Fase E) — the lazy, idempotent release of unpaid holds, same "clean up state the next time
/// something reads it" convention as <c>ISeatRepository.EnsureSeatsForVehicleAsync</c>. Real
/// SQLite-backed timing/filtering behavior, matching this codebase's persistence-test convention
/// for new repository methods (e.g. <see cref="ReservationRepositoryPaymentIdTests"/>).
/// </summary>
public class ReservationRepositoryHoldExpirationTests
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

    private static async Task<(Trip Trip, List<Seat> Seats)> SeedTripWithSeatsAsync(
        CheckbusDbContext db, int seatCount, CancellationToken cancellationToken)
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
            PlaceId = $"ChIJ-hold-expiration-test-{Guid.NewGuid():N}",
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

        var seats = Enumerable.Range(1, seatCount)
            .Select(number => new Seat { Id = Guid.NewGuid(), VehicleId = vehicle.Id, Number = number })
            .ToList();
        db.Seats.AddRange(seats);
        await db.SaveChangesAsync(cancellationToken);

        return (trip, seats);
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
    public async Task ReleaseExpiredHoldsAsync_PendientePagoOlderThanThreshold_FlipsToCancelada()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var (trip, seats) = await SeedTripWithSeatsAsync(db, seatCount: 1, cancellationToken);
        var passenger = CreatePassenger();
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = seats[0].Id,
            PassengerId = passenger.Id,
            Price = trip.Price,
            Status = ReservationStatus.PendientePago,
            CreatedAt = DateTime.UtcNow.AddMinutes(-20)
        };
        db.Passengers.Add(passenger);
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new ReservationRepository(db);
        await repository.ReleaseExpiredHoldsAsync(trip.Id, holdExpirationMinutes: 15, cancellationToken);

        var reloaded = await repository.GetByIdAsync(reservation.Id, cancellationToken);
        Assert.Equal(ReservationStatus.Cancelada, reloaded!.Status);
    }

    [Fact]
    public async Task ReleaseExpiredHoldsAsync_PendientePagoYoungerThanThreshold_LeavesUntouched()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var (trip, seats) = await SeedTripWithSeatsAsync(db, seatCount: 1, cancellationToken);
        var passenger = CreatePassenger();
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = seats[0].Id,
            PassengerId = passenger.Id,
            Price = trip.Price,
            Status = ReservationStatus.PendientePago,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        db.Passengers.Add(passenger);
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new ReservationRepository(db);
        await repository.ReleaseExpiredHoldsAsync(trip.Id, holdExpirationMinutes: 15, cancellationToken);

        var reloaded = await repository.GetByIdAsync(reservation.Id, cancellationToken);
        Assert.Equal(ReservationStatus.PendientePago, reloaded!.Status);
    }

    [Fact]
    public async Task ReleaseExpiredHoldsAsync_OldConfirmadaReservation_IsNeverTouched()
    {
        // Only PendientePago rows are a "hold" — an old Confirmada reservation (a paid,
        // honored seat) must never be released just because it's old.
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var (trip, seats) = await SeedTripWithSeatsAsync(db, seatCount: 1, cancellationToken);
        var passenger = CreatePassenger();
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = seats[0].Id,
            PassengerId = passenger.Id,
            Price = trip.Price,
            Status = ReservationStatus.Confirmada,
            CreatedAt = DateTime.UtcNow.AddMinutes(-120)
        };
        db.Passengers.Add(passenger);
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new ReservationRepository(db);
        await repository.ReleaseExpiredHoldsAsync(trip.Id, holdExpirationMinutes: 15, cancellationToken);

        var reloaded = await repository.GetByIdAsync(reservation.Id, cancellationToken);
        Assert.Equal(ReservationStatus.Confirmada, reloaded!.Status);
    }

    [Fact]
    public async Task ReleaseExpiredHoldsAsync_ScopedByTripId_DoesNotTouchAnotherTripsExpiredHold()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var (tripA, seatsA) = await SeedTripWithSeatsAsync(db, seatCount: 1, cancellationToken);
        var (tripB, seatsB) = await SeedTripWithSeatsAsync(db, seatCount: 1, cancellationToken);
        var passengerA = CreatePassenger();
        var passengerB = CreatePassenger();
        var reservationA = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = tripA.Id,
            SeatId = seatsA[0].Id,
            PassengerId = passengerA.Id,
            Price = tripA.Price,
            Status = ReservationStatus.PendientePago,
            CreatedAt = DateTime.UtcNow.AddMinutes(-20)
        };
        var reservationB = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = tripB.Id,
            SeatId = seatsB[0].Id,
            PassengerId = passengerB.Id,
            Price = tripB.Price,
            Status = ReservationStatus.PendientePago,
            CreatedAt = DateTime.UtcNow.AddMinutes(-20)
        };
        db.Passengers.AddRange(passengerA, passengerB);
        db.Reservations.AddRange(reservationA, reservationB);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new ReservationRepository(db);
        await repository.ReleaseExpiredHoldsAsync(tripA.Id, holdExpirationMinutes: 15, cancellationToken);

        var reloadedA = await repository.GetByIdAsync(reservationA.Id, cancellationToken);
        var reloadedB = await repository.GetByIdAsync(reservationB.Id, cancellationToken);
        Assert.Equal(ReservationStatus.Cancelada, reloadedA!.Status);
        Assert.Equal(ReservationStatus.PendientePago, reloadedB!.Status); // untouched — different Trip
    }

    [Fact]
    public async Task ReleaseExpiredHoldsAsync_NothingExpired_IsNoOp()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var (trip, _) = await SeedTripWithSeatsAsync(db, seatCount: 1, cancellationToken);

        var repository = new ReservationRepository(db);

        // No Reservation rows at all for this Trip — must complete without throwing.
        await repository.ReleaseExpiredHoldsAsync(trip.Id, holdExpirationMinutes: 15, cancellationToken);
    }

    [Fact]
    public async Task ReleaseExpiredHoldsAsync_ReleasingAnExpiredHold_FreesTheSeatForANewReservation()
    {
        // The whole point of this Fase: once released, the filtered unique index on
        // Reservation(TripId, SeatId) (which excludes Cancelada) no longer blocks a brand-new
        // Reservation on that same seat.
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var (trip, seats) = await SeedTripWithSeatsAsync(db, seatCount: 1, cancellationToken);
        var oldPassenger = CreatePassenger();
        var expiredHold = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = seats[0].Id,
            PassengerId = oldPassenger.Id,
            Price = trip.Price,
            Status = ReservationStatus.PendientePago,
            CreatedAt = DateTime.UtcNow.AddMinutes(-20)
        };
        db.Passengers.Add(oldPassenger);
        db.Reservations.Add(expiredHold);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new ReservationRepository(db);
        await repository.ReleaseExpiredHoldsAsync(trip.Id, holdExpirationMinutes: 15, cancellationToken);

        // A brand-new booking attempt on the same seat, by a new Payment/Passenger/Reservation
        // batch — must succeed now that the old hold has been released.
        var newPassenger = CreatePassenger();
        var newPayment = new Payment
        {
            Id = Guid.NewGuid(),
            ExternalOrderId = "mp-order-new",
            TotalAmount = new Money(trip.Price),
            Status = PaymentStatus.Pendiente,
            CreatedAt = DateTime.UtcNow
        };
        var newReservation = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = seats[0].Id,
            PassengerId = newPassenger.Id,
            Price = trip.Price,
            Status = ReservationStatus.PendientePago,
            PaymentId = newPayment.Id
        };

        await repository.CreateBatchAsync(newPayment, [newPassenger], [newReservation], cancellationToken);

        var active = await repository.GetByTripIdAsync(trip.Id, cancellationToken);
        Assert.Single(active);
        Assert.Equal(newReservation.Id, active[0].Id);
    }
}
