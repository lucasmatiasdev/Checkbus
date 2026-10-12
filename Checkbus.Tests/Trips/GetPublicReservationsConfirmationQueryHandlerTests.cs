using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Queries;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Entities.Seats;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.Tests.Trips;

/// <summary>
/// Proves <see cref="GetPublicReservationsConfirmationQueryHandler"/> — the public, read-only
/// confirmation page (odd/tasks/reservas-publicas.md R5) — resolves a batch of Reservation
/// ids, silently omits ids that don't resolve (never an error), and never exposes
/// Passenger.Email/Passenger.Phone. Fake-repository style, matching
/// <see cref="GetPublicTripSeatMapQueryHandlerTests"/>/<see cref="CreatePublicReservationCommandHandlerTests"/>.
/// </summary>
public class GetPublicReservationsConfirmationQueryHandlerTests
{
    private sealed class FakeReservationRepository(IReadOnlyList<Reservation> reservations) : IReservationRepository
    {
        public Task<IReadOnlyList<Reservation>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicReservationsConfirmationQueryHandler.");

        public Task CreateBatchAsync(
            IReadOnlyCollection<Passenger> passengers,
            IReadOnlyCollection<Reservation> newReservations,
            CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicReservationsConfirmationQueryHandler.");

        public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(reservations.FirstOrDefault(r => r.Id == id));

        public Task CancelAsync(Guid reservationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicReservationsConfirmationQueryHandler.");
    }

    private sealed class FakeSeatRepository(IReadOnlyList<Seat> seats) : ISeatRepository
    {
        public Task EnsureSeatsForVehicleAsync(Guid vehicleId, int capacity, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicReservationsConfirmationQueryHandler.");

        public Task<IReadOnlyList<Seat>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Seat>>(seats.Where(s => s.VehicleId == vehicleId).ToList());
    }

    private sealed class FakePassengerRepository(IReadOnlyList<Passenger> passengers) : IPassengerRepository
    {
        public Task<Passenger?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(passengers.FirstOrDefault(p => p.Id == id));
    }

    private sealed class FakeTripRepository(IReadOnlyList<Trip> trips) : ITripRepository
    {
        public Task AddAsync(Trip trip, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicReservationsConfirmationQueryHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(trips.FirstOrDefault(t => t.Id == id));

        public Task UpdateAsync(Trip trip, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicReservationsConfirmationQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicReservationsConfirmationQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicReservationsConfirmationQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicReservationsConfirmationQueryHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicReservationsConfirmationQueryHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicReservationsConfirmationQueryHandler.");
    }

    private sealed class FakeEventRepository(IReadOnlyList<Event> events) : IEventRepository
    {
        public Task AddAsync(Event @event, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicReservationsConfirmationQueryHandler.");

        public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(events.FirstOrDefault(e => e.Id == id));

        public Task<IReadOnlyList<Event>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicReservationsConfirmationQueryHandler.");

        public Task UpdateAsync(Event @event, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetPublicReservationsConfirmationQueryHandler.");
    }

    private static Trip CreateTrip(Guid vehicleId, Guid eventId) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        DriverId = Guid.NewGuid(),
        VehicleId = vehicleId,
        EventId = eventId,
        DepartureDate = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
        ArrivalDate = new DateTime(2026, 5, 1, 18, 0, 0, DateTimeKind.Utc),
        Capacity = 45,
        AvailableSeats = 45,
        Price = 15000m,
        Status = TripStatus.Publicado
    };

    private static Passenger CreatePassenger(string suffix) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Juan" + suffix,
        Surname = "Perez",
        DocumentType = DocumentType.DNI,
        DocumentNumber = "3011122" + suffix,
        Email = $"juan{suffix}@checkbus.test",
        Phone = "+54 11 5555-5555"
    };

    private sealed class Fixture
    {
        public required Trip Trip;
        public required Event Event;
        public required Seat Seat;
        public required Passenger Passenger;
        public required Reservation Reservation;
    }

    private static Fixture CreateFixture()
    {
        var vehicleId = Guid.NewGuid();
        var @event = new Event
        {
            Id = Guid.NewGuid(),
            Name = "Cosquin Rock",
            Type = EventType.Concierto,
            Date = new DateTime(2026, 5, 1),
            LocationId = Guid.NewGuid()
        };
        var trip = CreateTrip(vehicleId, @event.Id);
        var seat = new Seat { Id = Guid.NewGuid(), VehicleId = vehicleId, Number = 7 };
        var passenger = CreatePassenger("1");
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = seat.Id,
            PassengerId = passenger.Id,
            Price = trip.Price,
            Status = ReservationStatus.Confirmada
        };

        return new Fixture
        {
            Trip = trip,
            Event = @event,
            Seat = seat,
            Passenger = passenger,
            Reservation = reservation
        };
    }

    private static GetPublicReservationsConfirmationQueryHandler CreateHandler(
        IReadOnlyList<Reservation> reservations,
        IReadOnlyList<Seat> seats,
        IReadOnlyList<Passenger> passengers,
        IReadOnlyList<Trip> trips,
        IReadOnlyList<Event> events) => new(
            new FakeReservationRepository(reservations),
            new FakeSeatRepository(seats),
            new FakePassengerRepository(passengers),
            new FakeTripRepository(trips),
            new FakeEventRepository(events));

    [Fact]
    public async Task Handle_KnownReservationId_ReturnsFullConfirmationWithoutContactFields()
    {
        var fixture = CreateFixture();
        var handler = CreateHandler(
            [fixture.Reservation], [fixture.Seat], [fixture.Passenger], [fixture.Trip], [fixture.Event]);

        var result = await handler.Handle(
            new GetPublicReservationsConfirmationQuery { ReservationIds = [fixture.Reservation.Id] },
            TestContext.Current.CancellationToken);

        Assert.Single(result.Reservations);
        var dto = result.Reservations[0];
        Assert.Equal(fixture.Reservation.Id, dto.ReservationId);
        Assert.Equal(fixture.Seat.Number, dto.SeatNumber);
        Assert.Equal(fixture.Passenger.Name, dto.PassengerName);
        Assert.Equal(fixture.Passenger.Surname, dto.PassengerSurname);
        Assert.Equal(fixture.Passenger.DocumentType, dto.PassengerDocumentType);
        Assert.Equal(fixture.Passenger.DocumentNumber, dto.PassengerDocumentNumber);
        Assert.Equal(fixture.Reservation.Price, dto.Price);
        Assert.Equal(nameof(ReservationStatus.Confirmada), dto.Status);
        Assert.Equal(fixture.Event.Name, dto.EventName);
        Assert.Equal(fixture.Trip.DepartureDate, dto.DepartureDate);
    }

    [Fact]
    public void ReservationConfirmationDto_HasNoEmailOrPhoneProperty()
    {
        // Structural guarantee, not just "unset at runtime": the DTO's own shape must never
        // carry Passenger.Email/Passenger.Phone, since this page is reachable by anyone
        // holding a Reservation.Id.
        var properties = typeof(ReservationConfirmationDto).GetProperties().Select(p => p.Name);

        Assert.DoesNotContain("Email", properties);
        Assert.DoesNotContain("Phone", properties);
        Assert.DoesNotContain("PassengerEmail", properties);
        Assert.DoesNotContain("PassengerPhone", properties);
    }

    [Fact]
    public async Task Handle_MultipleIdsSameBatch_ReturnsAllInSameOrder()
    {
        var vehicleId = Guid.NewGuid();
        var @event = new Event
        {
            Id = Guid.NewGuid(),
            Name = "Lollapalooza",
            Type = EventType.Concierto,
            Date = new DateTime(2026, 6, 1),
            LocationId = Guid.NewGuid()
        };
        var trip = CreateTrip(vehicleId, @event.Id);
        var seat1 = new Seat { Id = Guid.NewGuid(), VehicleId = vehicleId, Number = 1 };
        var seat2 = new Seat { Id = Guid.NewGuid(), VehicleId = vehicleId, Number = 2 };
        var passenger1 = CreatePassenger("1");
        var passenger2 = CreatePassenger("2");
        var reservation1 = new Reservation
        {
            Id = Guid.NewGuid(), TripId = trip.Id, SeatId = seat1.Id, PassengerId = passenger1.Id, Price = trip.Price
        };
        var reservation2 = new Reservation
        {
            Id = Guid.NewGuid(), TripId = trip.Id, SeatId = seat2.Id, PassengerId = passenger2.Id, Price = trip.Price
        };

        var handler = CreateHandler(
            [reservation1, reservation2], [seat1, seat2], [passenger1, passenger2], [trip], [@event]);

        var result = await handler.Handle(
            new GetPublicReservationsConfirmationQuery { ReservationIds = [reservation1.Id, reservation2.Id] },
            TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Reservations.Count);
        Assert.Equal(reservation1.Id, result.Reservations[0].ReservationId);
        Assert.Equal(reservation2.Id, result.Reservations[1].ReservationId);
    }

    [Fact]
    public async Task Handle_PartialResolve_OmitsGarbageIdsWithoutError()
    {
        var fixture = CreateFixture();
        var handler = CreateHandler(
            [fixture.Reservation], [fixture.Seat], [fixture.Passenger], [fixture.Trip], [fixture.Event]);

        var result = await handler.Handle(
            new GetPublicReservationsConfirmationQuery
            {
                ReservationIds = [fixture.Reservation.Id, Guid.NewGuid(), Guid.NewGuid()]
            },
            TestContext.Current.CancellationToken);

        Assert.Single(result.Reservations);
        Assert.Equal(fixture.Reservation.Id, result.Reservations[0].ReservationId);
    }

    [Fact]
    public async Task Handle_AllGarbageIds_ReturnsEmptyResultWithoutError()
    {
        var handler = CreateHandler([], [], [], [], []);

        var result = await handler.Handle(
            new GetPublicReservationsConfirmationQuery { ReservationIds = [Guid.NewGuid(), Guid.NewGuid()] },
            TestContext.Current.CancellationToken);

        Assert.Empty(result.Reservations);
    }
}
