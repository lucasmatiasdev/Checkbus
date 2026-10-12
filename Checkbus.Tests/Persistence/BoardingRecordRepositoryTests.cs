using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.BoardingRecords;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Entities.Seats;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Boarding;
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Persistence;

/// <summary>
/// Proves <see cref="BoardingRecordRepository"/> (odd/tasks/reservas-publicas.md Fase 3 / A1):
/// <see cref="BoardingRecordRepository.EnsureForTripAsync"/>'s idempotency (mirrors
/// <see cref="SeatRepositoryTests"/>'s proof for <c>EnsureSeatsForVehicleAsync</c>),
/// <see cref="BoardingRecordRepository.CloseBoardingAsync"/>'s bulk update, and
/// <see cref="BoardingRecordRepository.ValidateAsync"/>'s state-transition guard. Same
/// SQLite-in-memory setup as <see cref="ReservationPersistenceTests"/>.
/// </summary>
public class BoardingRecordRepositoryTests
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

    private static Organization CreateOrganization() => new()
    {
        Id = Guid.NewGuid(),
        CUIT = "20-12345678-9",
        Name = "Checkbus Demo",
        Slug = "checkbus-demo-" + Guid.NewGuid(),
        LogoUrl = "",
        IsActive = true
    };

    private static Vehicle CreateVehicle(Guid organizationId) => new()
    {
        Id = Guid.NewGuid(),
        Brand = "Mercedes-Benz",
        Model = "O500",
        Year = 2020,
        Patent = "AB123CD",
        Capacity = 45,
        Mileage = 1000,
        Status = VehicleStatus.Activo,
        OrganizationId = organizationId,
        OwnerType = VehicleOwnerType.Organizacion
    };

    private static User CreateDriver(Guid organizationId) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Juan",
        Surname = "Perez",
        Email = "driver-" + Guid.NewGuid() + "@checkbus.test",
        PasswordHash = "hash",
        DocumentNumber = "30111222",
        Role = Role.Chofer,
        OrganizationId = organizationId
    };

    private static Location CreateLocation() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Estadio Monumental",
        Address = "Av. Pres. Figueroa Alcorta 7597",
        PlaceId = "ChIJ-boarding-test-" + Guid.NewGuid(),
        Latitude = -34.5453,
        Longitude = -58.4498
    };

    private static Passenger CreatePassenger() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Maria",
        Surname = "Gomez",
        DocumentType = DocumentType.DNI,
        DocumentNumber = "30111333",
        Email = "maria@checkbus.test",
        Phone = "+54 11 5555-5555"
    };

    /// <summary>Sets up one Trip with <paramref name="reservationCount"/> Reservations (one Seat
    /// + Passenger per Reservation), returning the persisted Trip and its Reservations.</summary>
    private static async Task<(Trip Trip, List<Reservation> Reservations)> SeedTripWithReservationsAsync(
        CheckbusDbContext db, int reservationCount, CancellationToken cancellationToken)
    {
        var organization = CreateOrganization();
        db.Organizations.Add(organization);
        var vehicle = CreateVehicle(organization.Id);
        db.Vehicles.Add(vehicle);
        var driver = CreateDriver(organization.Id);
        db.Users.Add(driver);
        var location = CreateLocation();
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
        await db.SaveChangesAsync(cancellationToken);

        var reservations = new List<Reservation>();
        for (var i = 0; i < reservationCount; i++)
        {
            var seat = new Seat { Id = Guid.NewGuid(), VehicleId = vehicle.Id, Number = i + 1 };
            var passenger = CreatePassenger();
            db.Seats.Add(seat);
            db.Passengers.Add(passenger);
            var reservation = new Reservation
            {
                Id = Guid.NewGuid(),
                TripId = trip.Id,
                SeatId = seat.Id,
                PassengerId = passenger.Id,
                Price = trip.Price
            };
            db.Reservations.Add(reservation);
            reservations.Add(reservation);
        }
        await db.SaveChangesAsync(cancellationToken);

        return (trip, reservations);
    }

    [Fact]
    public async Task EnsureForTripAsync_WhenNoBoardingRecordsExist_CreatesOnePendientePerReservation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var (trip, reservations) = await SeedTripWithReservationsAsync(db, 3, cancellationToken);

        var repository = new BoardingRecordRepository(db);
        await repository.EnsureForTripAsync(trip.Id, cancellationToken);

        var records = await db.BoardingRecords
            .Where(b => reservations.Select(r => r.Id).Contains(b.ReservationId))
            .ToListAsync(cancellationToken);

        Assert.Equal(3, records.Count);
        Assert.All(records, r => Assert.Equal(BoardingStatus.Pendiente, r.Status));
        Assert.Equal(reservations.Select(r => r.Id).OrderBy(id => id), records.Select(r => r.ReservationId).OrderBy(id => id));
    }

    [Fact]
    public async Task EnsureForTripAsync_CalledTwice_IsIdempotent_NoDuplicatesNoException()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var (trip, reservations) = await SeedTripWithReservationsAsync(db, 3, cancellationToken);

        var repository = new BoardingRecordRepository(db);
        await repository.EnsureForTripAsync(trip.Id, cancellationToken);

        // Second call must be a pure no-op read — no duplicate inserts, no unique-index
        // violation, still exactly one row per Reservation.
        await repository.EnsureForTripAsync(trip.Id, cancellationToken);

        var records = await db.BoardingRecords
            .Where(b => reservations.Select(r => r.Id).Contains(b.ReservationId))
            .ToListAsync(cancellationToken);

        Assert.Equal(3, records.Count);
        Assert.Equal(records.Select(r => r.ReservationId).Distinct().Count(), records.Count);
    }

    [Fact]
    public async Task EnsureForTripAsync_WhenSomeRecordsAlreadyExist_OnlyBackfillsMissingOnes()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var (trip, reservations) = await SeedTripWithReservationsAsync(db, 3, cancellationToken);

        db.BoardingRecords.Add(new BoardingRecord { Id = Guid.NewGuid(), ReservationId = reservations[0].Id });
        await db.SaveChangesAsync(cancellationToken);

        var repository = new BoardingRecordRepository(db);
        await repository.EnsureForTripAsync(trip.Id, cancellationToken);

        var records = await db.BoardingRecords
            .Where(b => reservations.Select(r => r.Id).Contains(b.ReservationId))
            .ToListAsync(cancellationToken);

        Assert.Equal(3, records.Count);
        Assert.Equal(reservations.Select(r => r.Id).OrderBy(id => id), records.Select(r => r.ReservationId).OrderBy(id => id));
    }

    [Fact]
    public async Task ValidateAsync_PendienteRecord_MovesToAbordadoWithMethodAndTimestamp()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var (trip, reservations) = await SeedTripWithReservationsAsync(db, 1, cancellationToken);
        var repository = new BoardingRecordRepository(db);
        await repository.EnsureForTripAsync(trip.Id, cancellationToken);

        var before = DateTime.UtcNow;
        await repository.ValidateAsync(reservations[0].Id, ValidationMethod.Codigo, cancellationToken);

        var record = await db.BoardingRecords.FirstAsync(b => b.ReservationId == reservations[0].Id, cancellationToken);
        Assert.Equal(BoardingStatus.Abordado, record.Status);
        Assert.Equal(ValidationMethod.Codigo, record.Method);
        Assert.NotNull(record.ValidatedAt);
        Assert.True(record.ValidatedAt >= before);
    }

    [Fact]
    public async Task ValidateAsync_AlreadyAbordado_ThrowsBoardingAlreadyValidated()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var (trip, reservations) = await SeedTripWithReservationsAsync(db, 1, cancellationToken);
        var repository = new BoardingRecordRepository(db);
        await repository.EnsureForTripAsync(trip.Id, cancellationToken);
        await repository.ValidateAsync(reservations[0].Id, ValidationMethod.Codigo, cancellationToken);

        await Assert.ThrowsAsync<BoardingAlreadyValidatedException>(() =>
            repository.ValidateAsync(reservations[0].Id, ValidationMethod.Busqueda, cancellationToken));
    }

    [Fact]
    public async Task ValidateAsync_ReservationWithNoBoardingRecord_ThrowsBoardingRecordNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new BoardingRecordRepository(db);

        await Assert.ThrowsAsync<BoardingRecordNotFoundException>(() =>
            repository.ValidateAsync(Guid.NewGuid(), ValidationMethod.Codigo, cancellationToken));
    }

    [Fact]
    public async Task GetByTripIdAsync_OnlyReturnsRecordsForReservationsOnThatTrip()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var (trip, reservations) = await SeedTripWithReservationsAsync(db, 2, cancellationToken);
        var (otherTrip, otherReservations) = await SeedTripWithReservationsAsync(db, 1, cancellationToken);

        var repository = new BoardingRecordRepository(db);
        await repository.EnsureForTripAsync(trip.Id, cancellationToken);
        await repository.EnsureForTripAsync(otherTrip.Id, cancellationToken);

        var records = await repository.GetByTripIdAsync(trip.Id, cancellationToken);

        Assert.Equal(2, records.Count);
        Assert.Equal(
            reservations.Select(r => r.Id).OrderBy(id => id),
            records.Select(r => r.ReservationId).OrderBy(id => id));
        Assert.DoesNotContain(records, r => otherReservations.Select(x => x.Id).Contains(r.ReservationId));
    }

    [Fact]
    public async Task CloseBoardingAsync_MovesOnlyPendienteRecordsToAusente_LeavesAbordadoUntouched()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var (trip, reservations) = await SeedTripWithReservationsAsync(db, 3, cancellationToken);
        var repository = new BoardingRecordRepository(db);
        await repository.EnsureForTripAsync(trip.Id, cancellationToken);
        await repository.ValidateAsync(reservations[0].Id, ValidationMethod.Codigo, cancellationToken);

        await repository.CloseBoardingAsync(trip.Id, cancellationToken);

        var records = await db.BoardingRecords
            .Where(b => reservations.Select(r => r.Id).Contains(b.ReservationId))
            .ToDictionaryAsync(b => b.ReservationId, cancellationToken);

        Assert.Equal(BoardingStatus.Abordado, records[reservations[0].Id].Status);
        Assert.Equal(BoardingStatus.Ausente, records[reservations[1].Id].Status);
        Assert.Equal(BoardingStatus.Ausente, records[reservations[2].Id].Status);
    }
}
