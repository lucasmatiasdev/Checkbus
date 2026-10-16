using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Events;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Payments;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Entities.Seats;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Checkbus.Tests.Trips;

/// <summary>
/// Proves <see cref="PaymentApprovedEventHandler"/> (odd/tasks/pagos-mercadopago.md Fase D5):
/// re-resolves each confirmed Reservation's Trip/Passenger/Seat from just the event's ids and
/// sends the right confirmation email, and a missing/invalid passenger for one reservation id
/// does not block sending for another.
/// </summary>
public class PaymentApprovedEventHandlerTests
{
    private sealed class FakeReservationRepository(IReadOnlyList<Reservation> reservations) : IReservationRepository
    {
        public Task<IReadOnlyList<Reservation>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by PaymentApprovedEventHandler.");

        public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(reservations.FirstOrDefault(r => r.Id == id));

        public Task CreateBatchAsync(Payment payment, IReadOnlyCollection<Passenger> passengers, IReadOnlyCollection<Reservation> newReservations, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by PaymentApprovedEventHandler.");

        public Task CancelAsync(Guid reservationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by PaymentApprovedEventHandler.");
    }

    private sealed class FakeTripRepository(IReadOnlyList<Trip> trips) : ITripRepository
    {
        public Task AddAsync(Trip trip, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by PaymentApprovedEventHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(trips.FirstOrDefault(t => t.Id == id));

        public Task UpdateAsync(Trip trip, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by PaymentApprovedEventHandler.");

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by PaymentApprovedEventHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by PaymentApprovedEventHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by PaymentApprovedEventHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by PaymentApprovedEventHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by PaymentApprovedEventHandler.");

        public Task UpdateStopDepartureTimesAsync(IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by PaymentApprovedEventHandler.");

        public Task<bool> SlugExistsAsync(Guid organizationId, string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by PaymentApprovedEventHandler.");

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by PaymentApprovedEventHandler.");
    }

    private sealed class FakePassengerRepository(IReadOnlyList<Passenger> passengers) : IPassengerRepository
    {
        public Task<Passenger?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(passengers.FirstOrDefault(p => p.Id == id));
    }

    private sealed class FakeSeatRepository(IReadOnlyList<Seat> seats) : ISeatRepository
    {
        public Task EnsureSeatsForVehicleAsync(Guid vehicleId, int capacity, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by PaymentApprovedEventHandler.");

        public Task<IReadOnlyList<Seat>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Seat>>(seats.Where(s => s.VehicleId == vehicleId).ToList());
    }

    private sealed class FakeEventRepository(Event? @event) : IEventRepository
    {
        public Task AddAsync(Event newEvent, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by PaymentApprovedEventHandler.");

        public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(@event is not null && @event.Id == id ? @event : null);

        public Task UpdateAsync(Event updatedEvent, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by PaymentApprovedEventHandler.");

        public Task<IReadOnlyList<Event>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by PaymentApprovedEventHandler.");
    }

    private sealed class FakeEmailService : IEmailService
    {
        public List<EmailMessage> SentMessages { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            SentMessages.Add(message);
            return Task.CompletedTask;
        }
    }

    private static Trip CreateTrip(Guid eventId) => new()
    {
        Id = Guid.NewGuid(),
        Slug = Guid.NewGuid().ToString(),
        OrganizationId = Guid.NewGuid(),
        DriverId = Guid.NewGuid(),
        VehicleId = Guid.NewGuid(),
        EventId = eventId,
        DepartureDate = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
        ArrivalDate = new DateTime(2026, 11, 1, 14, 0, 0, DateTimeKind.Utc),
        Capacity = 45,
        AvailableSeats = 44,
        Price = 1500m,
        Status = TripStatus.Publicado
    };

    private static Passenger CreatePassenger(string email = "pasajero@checkbus.test") => new()
    {
        Id = Guid.NewGuid(),
        Name = "Juan",
        Surname = "Perez",
        DocumentType = DocumentType.DNI,
        DocumentNumber = "30111222",
        Email = email,
        Phone = "+54 11 5555-5555"
    };

    [Fact]
    public async Task Handle_SendsConfirmationEmail_WithEventTripAndSeatDetails()
    {
        var @event = new Event
        {
            Id = Guid.NewGuid(),
            Name = "River vs Boca",
            Type = EventType.Partido,
            Date = DateTime.UtcNow.AddMonths(1),
            LocationId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid()
        };
        var trip = CreateTrip(@event.Id);
        var passenger = CreatePassenger();
        var seat = new Seat { Id = Guid.NewGuid(), VehicleId = trip.VehicleId, Number = 12 };
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = seat.Id,
            PassengerId = passenger.Id,
            Price = trip.Price,
            Status = ReservationStatus.Confirmada
        };

        var emailService = new FakeEmailService();
        var handler = new PaymentApprovedEventHandler(
            new FakeReservationRepository([reservation]),
            new FakeTripRepository([trip]),
            new FakePassengerRepository([passenger]),
            new FakeSeatRepository([seat]),
            new FakeEventRepository(@event),
            emailService,
            new() { BaseUrl = "https://checkbus.test" },
            NullLogger<PaymentApprovedEventHandler>.Instance);

        await handler.Handle(
            new PaymentApprovedEvent { PaymentId = Guid.NewGuid(), ReservationIds = [reservation.Id] },
            TestContext.Current.CancellationToken);

        var message = Assert.Single(emailService.SentMessages);
        Assert.Equal(passenger.Email, message.To);
        Assert.Contains("confirmada", message.Subject);
        Assert.Contains("River vs Boca", message.Subject);
        Assert.Contains("12", message.HtmlBody);
        Assert.Contains($"https://checkbus.test/reservas/confirmacion/{reservation.Id}", message.HtmlBody);
    }

    [Fact]
    public async Task Handle_OneReservationIdDoesNotResolve_StillSendsForTheOthers()
    {
        var @event = new Event
        {
            Id = Guid.NewGuid(),
            Name = "River vs Boca",
            Type = EventType.Partido,
            Date = DateTime.UtcNow.AddMonths(1),
            LocationId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid()
        };
        var trip = CreateTrip(@event.Id);
        var passenger = CreatePassenger();
        var seat = new Seat { Id = Guid.NewGuid(), VehicleId = trip.VehicleId, Number = 3 };
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = seat.Id,
            PassengerId = passenger.Id,
            Price = trip.Price,
            Status = ReservationStatus.Confirmada
        };
        var missingReservationId = Guid.NewGuid();

        var emailService = new FakeEmailService();
        var handler = new PaymentApprovedEventHandler(
            new FakeReservationRepository([reservation]),
            new FakeTripRepository([trip]),
            new FakePassengerRepository([passenger]),
            new FakeSeatRepository([seat]),
            new FakeEventRepository(@event),
            emailService,
            new(),
            NullLogger<PaymentApprovedEventHandler>.Instance);

        await handler.Handle(
            new PaymentApprovedEvent { PaymentId = Guid.NewGuid(), ReservationIds = [missingReservationId, reservation.Id] },
            TestContext.Current.CancellationToken);

        var message = Assert.Single(emailService.SentMessages);
        Assert.Equal(passenger.Email, message.To);
    }
}
