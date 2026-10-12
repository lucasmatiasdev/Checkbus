using Checkbus.ApiService.Application.Interfaces.Payments;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Commands;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Payments;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Entities.Seats;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Payments;
using Checkbus.ApiService.Domain.Exceptions.Reservations;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using Checkbus.ApiService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Checkbus.Tests.Trips;

/// <summary>
/// Proves <see cref="CreatePublicReservationCommandHandler"/> — R4's all-or-nothing batch
/// reservation create, reworked in odd/tasks/pagos-mercadopago.md Fase C into a payment-gated
/// seat hold — gates on <c>Trip.Status == Publicado</c> (same IDOR-safe rationale as
/// <see cref="GetPublicTripSeatMapQueryHandlerTests"/>), rejects a SeatId that doesn't belong to
/// the trip's vehicle, rejects an already-reserved seat via the fast pre-check, rejects a batch
/// with a duplicate SeatId as handler-level defense-in-depth, translates a DbUpdateException
/// from the single CreateBatchAsync save (the real double-booking guard — the unique index on
/// Reservation(TripId, SeatId)) into <see cref="SeatsNoLongerAvailableException"/>, creates
/// reservations as <see cref="ReservationStatus.PendientePago"/> instead of
/// <see cref="ReservationStatus.Confirmada"/>, persists one <see cref="Payment"/> row covering
/// the whole batch, and propagates <see cref="OrderCreationFailedException"/> without
/// persisting anything when Mercado Pago's own order-creation call fails. Fake-repository
/// style, matching <see cref="PublishTripCommandHandlerTests"/>/
/// <see cref="GetPublicTripSeatMapQueryHandlerTests"/>.
/// </summary>
public class CreatePublicReservationCommandHandlerTests
{
    private sealed class FakeTripRepository(Trip? trip) : ITripRepository
    {
        public Task AddAsync(Trip trip, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreatePublicReservationCommandHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(trip);

        public Task UpdateAsync(Trip trip, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreatePublicReservationCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreatePublicReservationCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreatePublicReservationCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreatePublicReservationCommandHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreatePublicReservationCommandHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreatePublicReservationCommandHandler.");

        public Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreatePublicReservationCommandHandler.");
    }

    private sealed class FakeSeatRepository(IReadOnlyList<Seat> seats) : ISeatRepository
    {
        public Task EnsureSeatsForVehicleAsync(Guid vehicleId, int capacity, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreatePublicReservationCommandHandler.");

        public Task<IReadOnlyList<Seat>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Seat>>(seats.Where(s => s.VehicleId == vehicleId).ToList());
    }

    private sealed class FakeReservationRepository(
        IReadOnlyList<Reservation> existingReservations, bool throwOnSave = false) : IReservationRepository
    {
        public Payment? SavedPayment { get; private set; }
        public List<Passenger>? SavedPassengers { get; private set; }
        public List<Reservation>? SavedReservations { get; private set; }

        public Task<IReadOnlyList<Reservation>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Reservation>>(existingReservations.Where(r => r.TripId == tripId).ToList());

        public Task CreateBatchAsync(
            Payment payment,
            IReadOnlyCollection<Passenger> passengers,
            IReadOnlyCollection<Reservation> reservations,
            CancellationToken cancellationToken)
        {
            if (throwOnSave)
            {
                // Simulates the real DB unique-constraint violation on
                // Reservation(TripId, SeatId) surfacing from SaveChangesAsync when a
                // concurrent submit wins the race between the handler's pre-check and this
                // call. By this point (Fase C ordering), the Mercado Pago order already
                // "succeeded" from the handler's point of view — only the local write fails.
                throw new DbUpdateException("Simulated unique-constraint violation.");
            }

            SavedPayment = payment;
            SavedPassengers = passengers.ToList();
            SavedReservations = reservations.ToList();
            return Task.CompletedTask;
        }

        public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreatePublicReservationCommandHandler.");

        public Task CancelAsync(Guid reservationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreatePublicReservationCommandHandler.");
    }

    // Fake IPaymentService — same public-list-for-assertions, NotSupportedException-for-unused
    // style as every other fake in this codebase (e.g. FakeEmailService below). Defaults to a
    // successful order creation so every test that doesn't care about the payment gateway
    // itself (seat-validation failures, the DbUpdateException race path) doesn't need to wire
    // one up explicitly.
    private sealed class FakePaymentService(
        CreateOrderResult? createOrderResult = null,
        Exception? createOrderException = null) : IPaymentService
    {
        public CreateOrderRequest? ReceivedRequest { get; private set; }

        public Task<CreateOrderResult> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken)
        {
            ReceivedRequest = request;

            if (createOrderException is not null)
            {
                throw createOrderException;
            }

            return Task.FromResult(
                createOrderResult ?? new CreateOrderResult("mp-order-default", "https://mercadopago.test/checkout/default"));
        }

        public Task<OrderStatusResult> GetOrderAsync(string externalOrderId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreatePublicReservationCommandHandler.");

        public Task RefundAsync(string externalOrderId, Money? amount, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreatePublicReservationCommandHandler.");
    }

    private static Event CreateEvent(string name = "Cosquin Rock") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Type = EventType.Concierto,
        Date = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
        LocationId = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid()
    };

    private static CreatePublicReservationCommandHandler CreateHandler(
        Trip? trip,
        IReadOnlyList<Seat> seats,
        FakeReservationRepository reservationRepository,
        FakePaymentService? paymentService = null) => new(
            new FakeTripRepository(trip),
            new FakeSeatRepository(seats),
            reservationRepository,
            paymentService ?? new FakePaymentService(),
            NullLogger<CreatePublicReservationCommandHandler>.Instance);

    private static Trip CreateTrip(Guid vehicleId, TripStatus status = TripStatus.Publicado, decimal price = 15000m) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        DriverId = Guid.NewGuid(),
        VehicleId = vehicleId,
        EventId = Guid.NewGuid(),
        DepartureDate = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
        ArrivalDate = new DateTime(2026, 5, 1, 18, 0, 0, DateTimeKind.Utc),
        Capacity = 45,
        AvailableSeats = 45,
        Price = price,
        Status = status
    };

    private static PassengerSeatRequest CreateEntry(Guid seatId, string suffix = "1") => new()
    {
        SeatId = seatId,
        Name = "Juan" + suffix,
        Surname = "Perez",
        DocumentType = DocumentType.DNI,
        DocumentNumber = "3011122" + suffix,
        Email = $"juan{suffix}@checkbus.test",
        Phone = "+54 11 5555-5555"
    };

    [Fact]
    public async Task Handle_SingleSeat_CreatesOnePassengerAndOnePendientePagoReservation()
    {
        var vehicleId = Guid.NewGuid();
        var trip = CreateTrip(vehicleId);
        var seats = new List<Seat> { new() { Id = Guid.NewGuid(), VehicleId = vehicleId, Number = 1 } };
        var reservationRepository = new FakeReservationRepository([]);

        var handler = CreateHandler(trip, seats, reservationRepository);

        var result = await handler.Handle(
            new CreatePublicReservationCommand { TripId = trip.Id, Passengers = [CreateEntry(seats[0].Id)] },
            TestContext.Current.CancellationToken);

        Assert.Single(result.Reservations);
        Assert.Equal(1, result.Reservations[0].SeatNumber);
        Assert.Equal(trip.Price, result.Reservations[0].Price);
        Assert.NotNull(reservationRepository.SavedPassengers);
        Assert.Single(reservationRepository.SavedPassengers!);
        Assert.Single(reservationRepository.SavedReservations!);
        // The core Fase C behavior change: a hold, not an immediate confirmation.
        Assert.Equal(ReservationStatus.PendientePago, reservationRepository.SavedReservations![0].Status);
    }

    [Fact]
    public async Task Handle_MultiSeatFamilyOfThree_CreatesThreePassengersAndThreePendientePagoReservations()
    {
        var vehicleId = Guid.NewGuid();
        var trip = CreateTrip(vehicleId);
        var seats = new List<Seat>
        {
            new() { Id = Guid.NewGuid(), VehicleId = vehicleId, Number = 1 },
            new() { Id = Guid.NewGuid(), VehicleId = vehicleId, Number = 2 },
            new() { Id = Guid.NewGuid(), VehicleId = vehicleId, Number = 3 }
        };
        var reservationRepository = new FakeReservationRepository([]);

        var handler = CreateHandler(trip, seats, reservationRepository);

        var command = new CreatePublicReservationCommand
        {
            TripId = trip.Id,
            Passengers =
            [
                CreateEntry(seats[0].Id, "1"),
                CreateEntry(seats[1].Id, "2"),
                CreateEntry(seats[2].Id, "3")
            ]
        };

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.Equal(3, result.Reservations.Count);
        Assert.Equal(3, reservationRepository.SavedPassengers!.Count);
        Assert.Equal(3, reservationRepository.SavedReservations!.Count);
        Assert.All(reservationRepository.SavedReservations!, r =>
        {
            Assert.Equal(trip.Price, r.Price);
            Assert.Equal(ReservationStatus.PendientePago, r.Status);
        });
    }

    [Fact]
    public async Task Handle_TripNotFound_ThrowsTripNotFound()
    {
        var handler = CreateHandler(null, [], new FakeReservationRepository([]));

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(
                new CreatePublicReservationCommand { TripId = Guid.NewGuid(), Passengers = [CreateEntry(Guid.NewGuid())] },
                TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(TripStatus.Programado)]
    [InlineData(TripStatus.EnProceso)]
    [InlineData(TripStatus.Completado)]
    [InlineData(TripStatus.Cancelado)]
    public async Task Handle_TripNotPublicado_ThrowsSameTripNotFound(TripStatus status)
    {
        var vehicleId = Guid.NewGuid();
        var trip = CreateTrip(vehicleId, status);
        var seat = new Seat { Id = Guid.NewGuid(), VehicleId = vehicleId, Number = 1 };

        var handler = CreateHandler(trip, [seat], new FakeReservationRepository([]));

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(
                new CreatePublicReservationCommand { TripId = trip.Id, Passengers = [CreateEntry(seat.Id)] },
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_SeatBelongsToDifferentVehicle_ThrowsSeatsNoLongerAvailable()
    {
        var vehicleId = Guid.NewGuid();
        var trip = CreateTrip(vehicleId);
        // A SeatId that exists but belongs to a completely different vehicle — must not be
        // silently accepted.
        var foreignSeat = new Seat { Id = Guid.NewGuid(), VehicleId = Guid.NewGuid(), Number = 1 };

        var handler = CreateHandler(trip, [foreignSeat], new FakeReservationRepository([]));

        await Assert.ThrowsAsync<SeatsNoLongerAvailableException>(() =>
            handler.Handle(
                new CreatePublicReservationCommand { TripId = trip.Id, Passengers = [CreateEntry(foreignSeat.Id)] },
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_DuplicateSeatInSameRequest_ThrowsSeatsNoLongerAvailable()
    {
        // Defense-in-depth: CreatePublicReservationCommandValidator already rejects this at
        // the FluentValidation layer, but the handler must not silently misbehave (e.g.
        // double-counting) if that rule were ever bypassed — a duplicate SeatId collides with
        // the existing-reservation pre-check logic and is rejected the same way an
        // already-taken seat would be.
        var vehicleId = Guid.NewGuid();
        var trip = CreateTrip(vehicleId);
        var seat = new Seat { Id = Guid.NewGuid(), VehicleId = vehicleId, Number = 1 };

        var command = new CreatePublicReservationCommand
        {
            TripId = trip.Id,
            Passengers = [CreateEntry(seat.Id, "1"), CreateEntry(seat.Id, "2")]
        };

        // Not already reserved, and both entries resolve to the same real seat — the handler
        // builds two Reservation rows for the same (TripId, SeatId) pair, which the batch save
        // would reject via the unique index; here that is simulated by throwOnSave. The order
        // call still happens (and succeeds) before this — the real failure is the local save.
        var reservationRepository = new FakeReservationRepository([], throwOnSave: true);
        var handlerWithThrow = CreateHandler(trip, [seat], reservationRepository);

        await Assert.ThrowsAsync<SeatsNoLongerAvailableException>(() =>
            handlerWithThrow.Handle(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_SeatAlreadyReserved_PreCheckThrowsSeatsNoLongerAvailable()
    {
        var vehicleId = Guid.NewGuid();
        var trip = CreateTrip(vehicleId);
        var seat = new Seat { Id = Guid.NewGuid(), VehicleId = vehicleId, Number = 1 };
        var existingReservation = new Reservation
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            SeatId = seat.Id,
            PassengerId = Guid.NewGuid(),
            Price = trip.Price
        };

        var handler = CreateHandler(trip, [seat], new FakeReservationRepository([existingReservation]));

        await Assert.ThrowsAsync<SeatsNoLongerAvailableException>(() =>
            handler.Handle(
                new CreatePublicReservationCommand { TripId = trip.Id, Passengers = [CreateEntry(seat.Id)] },
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_SaveThrowsDbUpdateException_RethrowsAsSeatsNoLongerAvailable()
    {
        // Proves the race path: the pre-check passes (nothing reserved yet from this
        // handler's point of view), the Mercado Pago order call succeeds, but the single
        // CreateBatchAsync save throws DbUpdateException (simulating another visitor's
        // concurrent submit winning the race via the real unique index on
        // Reservation(TripId, SeatId)) — the handler must translate that into the same
        // domain-level failure, not let it bubble up as a raw DbUpdateException / 500.
        var vehicleId = Guid.NewGuid();
        var trip = CreateTrip(vehicleId);
        var seat = new Seat { Id = Guid.NewGuid(), VehicleId = vehicleId, Number = 1 };
        var reservationRepository = new FakeReservationRepository([], throwOnSave: true);

        var handler = CreateHandler(trip, [seat], reservationRepository);

        await Assert.ThrowsAsync<SeatsNoLongerAvailableException>(() =>
            handler.Handle(
                new CreatePublicReservationCommand { TripId = trip.Id, Passengers = [CreateEntry(seat.Id)] },
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_SingleSeat_PersistsPaymentWithTotalAmountAndExternalOrderId()
    {
        var vehicleId = Guid.NewGuid();
        var trip = CreateTrip(vehicleId, price: 15000m);
        var seat = new Seat { Id = Guid.NewGuid(), VehicleId = vehicleId, Number = 1 };
        var reservationRepository = new FakeReservationRepository([]);
        var paymentService = new FakePaymentService(
            new CreateOrderResult("mp-order-123", "https://mercadopago.test/checkout/123"));

        var handler = CreateHandler(trip, [seat], reservationRepository, paymentService);

        await handler.Handle(
            new CreatePublicReservationCommand { TripId = trip.Id, Passengers = [CreateEntry(seat.Id)] },
            TestContext.Current.CancellationToken);

        Assert.NotNull(reservationRepository.SavedPayment);
        Assert.Equal(15000m, reservationRepository.SavedPayment!.TotalAmount.Amount);
        Assert.Equal("mp-order-123", reservationRepository.SavedPayment.ExternalOrderId);
        Assert.Equal(PaymentStatus.Pendiente, reservationRepository.SavedPayment.Status);
    }

    [Fact]
    public async Task Handle_MultiSeat_PaymentTotalAmountIsSumOfTripPriceAcrossAllSeats()
    {
        var vehicleId = Guid.NewGuid();
        var trip = CreateTrip(vehicleId, price: 15000m);
        var seats = new List<Seat>
        {
            new() { Id = Guid.NewGuid(), VehicleId = vehicleId, Number = 1 },
            new() { Id = Guid.NewGuid(), VehicleId = vehicleId, Number = 2 },
            new() { Id = Guid.NewGuid(), VehicleId = vehicleId, Number = 3 }
        };
        var reservationRepository = new FakeReservationRepository([]);

        var handler = CreateHandler(trip, seats, reservationRepository);

        await handler.Handle(
            new CreatePublicReservationCommand
            {
                TripId = trip.Id,
                Passengers =
                [
                    CreateEntry(seats[0].Id, "1"),
                    CreateEntry(seats[1].Id, "2"),
                    CreateEntry(seats[2].Id, "3")
                ]
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(45000m, reservationRepository.SavedPayment!.TotalAmount.Amount);
    }

    [Fact]
    public async Task Handle_CallsCreateOrderAsync_WithPaymentIdAsExternalReference()
    {
        // The whole point of generating Payment.Id client-side before the gateway call
        // (Fase C design decision): it becomes external_reference up front, so Fase D's
        // webhook can resolve a Mercado Pago notification back to this exact Payment row.
        // Round-trip proof: the value passed as ExternalReference must equal the Id on the
        // Payment row that actually got persisted.
        var vehicleId = Guid.NewGuid();
        var trip = CreateTrip(vehicleId);
        var seat = new Seat { Id = Guid.NewGuid(), VehicleId = vehicleId, Number = 1 };
        var reservationRepository = new FakeReservationRepository([]);
        var paymentService = new FakePaymentService();

        var handler = CreateHandler(trip, [seat], reservationRepository, paymentService);

        await handler.Handle(
            new CreatePublicReservationCommand { TripId = trip.Id, Passengers = [CreateEntry(seat.Id)] },
            TestContext.Current.CancellationToken);

        Assert.NotNull(paymentService.ReceivedRequest);
        Assert.Equal(
            reservationRepository.SavedPayment!.Id.ToString(),
            paymentService.ReceivedRequest!.ExternalReference);
    }

    [Fact]
    public async Task Handle_ReturnsCheckoutUrlFromOrderResult()
    {
        var vehicleId = Guid.NewGuid();
        var trip = CreateTrip(vehicleId);
        var seat = new Seat { Id = Guid.NewGuid(), VehicleId = vehicleId, Number = 1 };
        var paymentService = new FakePaymentService(
            new CreateOrderResult("mp-order-123", "https://mercadopago.test/checkout/123"));

        var handler = CreateHandler(trip, [seat], new FakeReservationRepository([]), paymentService);

        var result = await handler.Handle(
            new CreatePublicReservationCommand { TripId = trip.Id, Passengers = [CreateEntry(seat.Id)] },
            TestContext.Current.CancellationToken);

        Assert.Equal("https://mercadopago.test/checkout/123", result.CheckoutUrl);
    }

    [Fact]
    public async Task Handle_OrderResultWithNullCheckoutUrl_StillPersistsAndReturnsNullCheckoutUrl()
    {
        // Documented-rare case (CreateOrderResult.CheckoutUrl's own doc comment) — must not
        // block the whole flow, just surface null rather than throwing or silently fabricating
        // a URL.
        var vehicleId = Guid.NewGuid();
        var trip = CreateTrip(vehicleId);
        var seat = new Seat { Id = Guid.NewGuid(), VehicleId = vehicleId, Number = 1 };
        var reservationRepository = new FakeReservationRepository([]);
        var paymentService = new FakePaymentService(new CreateOrderResult("mp-order-123", null));

        var handler = CreateHandler(trip, [seat], reservationRepository, paymentService);

        var result = await handler.Handle(
            new CreatePublicReservationCommand { TripId = trip.Id, Passengers = [CreateEntry(seat.Id)] },
            TestContext.Current.CancellationToken);

        Assert.Null(result.CheckoutUrl);
        Assert.NotNull(reservationRepository.SavedPayment);
        Assert.Equal("mp-order-123", reservationRepository.SavedPayment!.ExternalOrderId);
    }

    [Fact]
    public async Task Handle_OrderCreationFails_PropagatesAndPersistsNothing()
    {
        // The Fase C ordering's whole purpose: Payment.Id is generated and CreateOrderAsync is
        // called BEFORE any DB write, so a gateway failure has nothing to roll back — the
        // exception propagates untouched and CreateBatchAsync must never have been called.
        var vehicleId = Guid.NewGuid();
        var trip = CreateTrip(vehicleId);
        var seat = new Seat { Id = Guid.NewGuid(), VehicleId = vehicleId, Number = 1 };
        var reservationRepository = new FakeReservationRepository([]);
        var paymentService = new FakePaymentService(createOrderException: new OrderCreationFailedException());

        var handler = CreateHandler(trip, [seat], reservationRepository, paymentService);

        await Assert.ThrowsAsync<OrderCreationFailedException>(() =>
            handler.Handle(
                new CreatePublicReservationCommand { TripId = trip.Id, Passengers = [CreateEntry(seat.Id)] },
                TestContext.Current.CancellationToken));

        Assert.Null(reservationRepository.SavedPayment);
        Assert.Null(reservationRepository.SavedPassengers);
        Assert.Null(reservationRepository.SavedReservations);
    }
}
