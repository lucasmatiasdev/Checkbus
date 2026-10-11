using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Entities.Seats;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Persistence;

/// <summary>
/// Fase 1 of reservas-publicas has no repositories yet (R1 is domain + persistence only) —
/// these tests exercise <see cref="Seat"/>, <see cref="Passenger"/> and <see cref="Reservation"/>
/// directly against <see cref="CheckbusDbContext"/>, same SQLite-in-memory setup used by every
/// other persistence test in this folder (e.g. VehicleDocumentRepositoryTests).
/// </summary>
public class ReservationPersistenceTests
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

    private static Organization CreateOrganization(string slug) => new()
    {
        Id = Guid.NewGuid(),
        CUIT = "20-12345678-9",
        Name = "Checkbus Demo",
        Slug = slug,
        LogoUrl = "",
        IsActive = true
    };

    private static Vehicle CreateVehicle(Guid organizationId, string patent) => new()
    {
        Id = Guid.NewGuid(),
        Brand = "Mercedes-Benz",
        Model = "O500",
        Year = 2020,
        Patent = patent,
        Capacity = 45,
        Mileage = 1000,
        Status = VehicleStatus.Activo,
        OrganizationId = organizationId,
        OwnerType = VehicleOwnerType.Organizacion
    };

    private static Passenger CreatePassenger(string documentNumber) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Juan",
        Surname = "Perez",
        DocumentType = DocumentType.DNI,
        DocumentNumber = documentNumber,
        Email = $"{documentNumber}@checkbus.test",
        Phone = "+54 11 5555-5555"
    };

    [Fact]
    public async Task Seat_PersistsAndRoundTrips()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        db.Organizations.Add(organization);
        var vehicle = CreateVehicle(organization.Id, "AB123CD");
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(cancellationToken);

        var seat = new Seat { Id = Guid.NewGuid(), VehicleId = vehicle.Id, Number = 1 };
        db.Seats.Add(seat);
        await db.SaveChangesAsync(cancellationToken);

        var reloaded = await db.Seats.FirstAsync(s => s.Id == seat.Id, cancellationToken);
        Assert.Equal(vehicle.Id, reloaded.VehicleId);
        Assert.Equal(1, reloaded.Number);
    }

    [Fact]
    public async Task Seat_UniqueIndex_DuplicateVehicleIdAndNumber_IsRejected()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        db.Organizations.Add(organization);
        var vehicle = CreateVehicle(organization.Id, "AB123CD");
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(cancellationToken);

        db.Seats.Add(new Seat { Id = Guid.NewGuid(), VehicleId = vehicle.Id, Number = 1 });
        await db.SaveChangesAsync(cancellationToken);

        db.Seats.Add(new Seat { Id = Guid.NewGuid(), VehicleId = vehicle.Id, Number = 1 });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(cancellationToken));
    }

    [Fact]
    public async Task Passenger_AllowsDuplicateDocumentNumber_NoDedup()
    {
        // Explicit design decision in odd/tasks/reservas-publicas.md: Passenger is NOT a
        // reusable catalog — a fresh row is created on every reservation even when the
        // DocumentNumber repeats. No unique index exists on DocumentNumber.
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        db.Passengers.Add(CreatePassenger("30111222"));
        db.Passengers.Add(CreatePassenger("30111222"));
        await db.SaveChangesAsync(cancellationToken);

        var count = await db.Passengers.CountAsync(p => p.DocumentNumber == "30111222", cancellationToken);
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Passenger_DocumentType_RoundTripsViaStringConversion()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var passenger = CreatePassenger("30111222");
        passenger.DocumentType = DocumentType.Pasaporte;
        db.Passengers.Add(passenger);
        await db.SaveChangesAsync(cancellationToken);

        var reloaded = await db.Passengers.FirstAsync(p => p.Id == passenger.Id, cancellationToken);
        Assert.Equal(DocumentType.Pasaporte, reloaded.DocumentType);
    }

    [Fact]
    public async Task Reservation_PersistsWithConfirmadaStatus_AndRoundTripsViaStringConversion()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        db.Organizations.Add(organization);
        var vehicle = CreateVehicle(organization.Id, "AB123CD");
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(cancellationToken);

        var driver = new User
        {
            Id = Guid.NewGuid(),
            Name = "Juan",
            Surname = "Perez",
            Email = "driver@checkbus.test",
            PasswordHash = "hash",
            DocumentNumber = "30111222",
            Role = Role.Chofer,
            OrganizationId = organization.Id
        };
        var location = new Location
        {
            Id = Guid.NewGuid(),
            Name = "Estadio Monumental",
            Address = "Av. Pres. Figueroa Alcorta 7597",
            PlaceId = "ChIJ-reservation-test",
            Latitude = -34.5453,
            Longitude = -58.4498
        };
        db.Users.Add(driver);
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
        var seat = new Seat { Id = Guid.NewGuid(), VehicleId = vehicle.Id, Number = 1 };
        db.Seats.Add(seat);
        var passenger = CreatePassenger("30111222");
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

        var reloaded = await db.Reservations.FirstAsync(r => r.Id == reservation.Id, cancellationToken);
        Assert.Equal(ReservationStatus.Confirmada, reloaded.Status);
        Assert.Equal(trip.Price, reloaded.Price);
        Assert.Equal(seat.Id, reloaded.SeatId);
        Assert.Equal(passenger.Id, reloaded.PassengerId);
    }

    [Fact]
    public async Task Reservation_UniqueIndex_DuplicateTripIdAndSeatId_IsRejected()
    {
        // The real double-booking guard added in R4 (odd/tasks/reservas-publicas.md): a
        // unique index on Reservation(TripId, SeatId). Two different Passengers trying to
        // book the same Seat on the same Trip must be rejected at the DB level even if an
        // application-level pre-check were somehow bypassed.
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        db.Organizations.Add(organization);
        var vehicle = CreateVehicle(organization.Id, "AB123CD");
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(cancellationToken);

        var driver = new User
        {
            Id = Guid.NewGuid(),
            Name = "Juan",
            Surname = "Perez",
            Email = "driver2@checkbus.test",
            PasswordHash = "hash",
            DocumentNumber = "30111223",
            Role = Role.Chofer,
            OrganizationId = organization.Id
        };
        var location = new Location
        {
            Id = Guid.NewGuid(),
            Name = "Estadio Monumental",
            Address = "Av. Pres. Figueroa Alcorta 7597",
            PlaceId = "ChIJ-reservation-unique-test",
            Latitude = -34.5453,
            Longitude = -58.4498
        };
        db.Users.Add(driver);
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
        var seat = new Seat { Id = Guid.NewGuid(), VehicleId = vehicle.Id, Number = 1 };
        db.Seats.Add(seat);
        var firstPassenger = CreatePassenger("30111222");
        var secondPassenger = CreatePassenger("30111224");
        db.Passengers.Add(firstPassenger);
        db.Passengers.Add(secondPassenger);
        await db.SaveChangesAsync(cancellationToken);

        db.Reservations.Add(new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = seat.Id,
            PassengerId = firstPassenger.Id,
            Price = trip.Price
        });
        await db.SaveChangesAsync(cancellationToken);

        db.Reservations.Add(new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = seat.Id,
            PassengerId = secondPassenger.Id,
            Price = trip.Price
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(cancellationToken));
    }

    [Fact]
    public async Task Reservation_FilteredUniqueIndex_CanceladaRow_DoesNotBlockNewConfirmadaOnSameSeat()
    {
        // The actual regression this phase (odd/tasks/reservas-publicas.md Fase 4 / C1) is
        // built to prevent: once a Reservation is Cancelada, its Seat must be re-bookable on
        // the same Trip without tripping the unique index on (TripId, SeatId) — that index is
        // now filtered (WHERE "Status" <> 'Cancelada') specifically so this insert succeeds.
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        db.Organizations.Add(organization);
        var vehicle = CreateVehicle(organization.Id, "AB123CD");
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(cancellationToken);

        var driver = new User
        {
            Id = Guid.NewGuid(),
            Name = "Juan",
            Surname = "Perez",
            Email = "driver3@checkbus.test",
            PasswordHash = "hash",
            DocumentNumber = "30111225",
            Role = Role.Chofer,
            OrganizationId = organization.Id
        };
        var location = new Location
        {
            Id = Guid.NewGuid(),
            Name = "Estadio Monumental",
            Address = "Av. Pres. Figueroa Alcorta 7597",
            PlaceId = "ChIJ-reservation-filtered-index-test",
            Latitude = -34.5453,
            Longitude = -58.4498
        };
        db.Users.Add(driver);
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
        var seat = new Seat { Id = Guid.NewGuid(), VehicleId = vehicle.Id, Number = 1 };
        db.Seats.Add(seat);
        var firstPassenger = CreatePassenger("30111222");
        var secondPassenger = CreatePassenger("30111226");
        db.Passengers.Add(firstPassenger);
        db.Passengers.Add(secondPassenger);
        await db.SaveChangesAsync(cancellationToken);

        var cancelledReservation = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = seat.Id,
            PassengerId = firstPassenger.Id,
            Price = trip.Price,
            Status = ReservationStatus.Cancelada
        };
        db.Reservations.Add(cancelledReservation);
        await db.SaveChangesAsync(cancellationToken);

        // A brand-new, still-Confirmada Reservation for the SAME (TripId, SeatId) must not
        // throw, because the cancelled row above no longer counts against the unique index.
        db.Reservations.Add(new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = seat.Id,
            PassengerId = secondPassenger.Id,
            Price = trip.Price
        });
        await db.SaveChangesAsync(cancellationToken);

        var activeCount = await db.Reservations.CountAsync(
            r => r.TripId == trip.Id && r.SeatId == seat.Id && r.Status == ReservationStatus.Confirmada,
            cancellationToken);
        Assert.Equal(1, activeCount);
    }

    [Fact]
    public async Task ReservationRepository_CancelAsync_SetsStatusToCancelada_AndFreesTheSeatForANewReservation()
    {
        // Repository-level equivalent of the EF test above (odd/tasks/reservas-publicas.md
        // Fase 4 / C6): going through ReservationRepository.CancelAsync (not raw EF) must
        // produce the exact same persisted effect — Status flips to Cancelada and the
        // filtered unique index no longer blocks a new Reservation on the same seat.
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        db.Organizations.Add(organization);
        var vehicle = CreateVehicle(organization.Id, "AB123CD");
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(cancellationToken);

        var driver = new User
        {
            Id = Guid.NewGuid(),
            Name = "Juan",
            Surname = "Perez",
            Email = "driver4@checkbus.test",
            PasswordHash = "hash",
            DocumentNumber = "30111227",
            Role = Role.Chofer,
            OrganizationId = organization.Id
        };
        var location = new Location
        {
            Id = Guid.NewGuid(),
            Name = "Estadio Monumental",
            Address = "Av. Pres. Figueroa Alcorta 7597",
            PlaceId = "ChIJ-reservation-cancelasync-test",
            Latitude = -34.5453,
            Longitude = -58.4498
        };
        db.Users.Add(driver);
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
        var seat = new Seat { Id = Guid.NewGuid(), VehicleId = vehicle.Id, Number = 1 };
        db.Seats.Add(seat);
        var firstPassenger = CreatePassenger("30111222");
        var secondPassenger = CreatePassenger("30111228");
        db.Passengers.Add(firstPassenger);
        db.Passengers.Add(secondPassenger);
        await db.SaveChangesAsync(cancellationToken);

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = seat.Id,
            PassengerId = firstPassenger.Id,
            Price = trip.Price
        };
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new ReservationRepository(db);
        await repository.CancelAsync(reservation.Id, cancellationToken);

        var reloaded = await db.Reservations.FirstAsync(r => r.Id == reservation.Id, cancellationToken);
        Assert.Equal(ReservationStatus.Cancelada, reloaded.Status);

        // The freed seat must accept a brand-new Confirmada Reservation on the same
        // (TripId, SeatId) without tripping the filtered unique index.
        db.Reservations.Add(new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = seat.Id,
            PassengerId = secondPassenger.Id,
            Price = trip.Price
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    [Fact]
    public async Task ReservationRepository_GetByTripIdAsync_ExcludesCanceladaReservations()
    {
        // Follow-up to Fase 4 / C1 (odd/tasks/reservas-publicas.md): a Cancelada Reservation
        // must not count as occupying its Seat. GetByTripIdAsync now filters it out at the
        // repository level so every caller (GetPublicTripSeatMapQueryHandler,
        // GetPublicTripDetailQueryHandler, CreatePublicReservationCommandHandler's pre-check)
        // stays correct without each one re-deriving "active" itself.
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        db.Organizations.Add(organization);
        var vehicle = CreateVehicle(organization.Id, "AB123CD");
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(cancellationToken);

        var driver = new User
        {
            Id = Guid.NewGuid(),
            Name = "Juan",
            Surname = "Perez",
            Email = "driver5@checkbus.test",
            PasswordHash = "hash",
            DocumentNumber = "30111229",
            Role = Role.Chofer,
            OrganizationId = organization.Id
        };
        var location = new Location
        {
            Id = Guid.NewGuid(),
            Name = "Estadio Monumental",
            Address = "Av. Pres. Figueroa Alcorta 7597",
            PlaceId = "ChIJ-reservation-getbytripid-test",
            Latitude = -34.5453,
            Longitude = -58.4498
        };
        db.Users.Add(driver);
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
        var cancelledSeat = new Seat { Id = Guid.NewGuid(), VehicleId = vehicle.Id, Number = 1 };
        var activeSeat = new Seat { Id = Guid.NewGuid(), VehicleId = vehicle.Id, Number = 2 };
        db.Seats.Add(cancelledSeat);
        db.Seats.Add(activeSeat);
        var cancelledPassenger = CreatePassenger("30111230");
        var activePassenger = CreatePassenger("30111231");
        db.Passengers.Add(cancelledPassenger);
        db.Passengers.Add(activePassenger);
        await db.SaveChangesAsync(cancellationToken);

        var cancelledReservation = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = cancelledSeat.Id,
            PassengerId = cancelledPassenger.Id,
            Price = trip.Price,
            Status = ReservationStatus.Cancelada
        };
        var activeReservation = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = activeSeat.Id,
            PassengerId = activePassenger.Id,
            Price = trip.Price
        };
        db.Reservations.Add(cancelledReservation);
        db.Reservations.Add(activeReservation);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new ReservationRepository(db);
        var reservations = await repository.GetByTripIdAsync(trip.Id, cancellationToken);

        Assert.Single(reservations);
        Assert.Equal(activeReservation.Id, reservations[0].Id);
    }
}
