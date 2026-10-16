using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Payments;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Commands;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Payments;
using Checkbus.ApiService.Domain.Entities.Refunds;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Entities.Seats;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using Checkbus.ApiService.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

namespace Checkbus.Tests.Trips;

/// <summary>
/// Proves <see cref="CancelTripCommandHandler"/> (CU-05): tenant-scoped IDOR-safe load (mirrors
/// PublishTripCommandHandlerTests), the two independent cancellation-blocking reasons (invalid
/// status / inside the 5-day notice window), cancelling every still-active Reservation on the
/// Trip, refund grouped ONCE PER PAYMENT (not once per Reservation, unlike
/// CancelReservationCommandHandler, since a trip-level cancel never leaves an active "sibling"
/// seat), and the best-effort per-passenger cancellation email.
/// </summary>
public class CancelTripCommandHandlerTests
{
    private sealed class FakeCurrentUserService(Guid? organizationId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => "Administrador";
        public string? Email => "admin@checkbus-demo.com";
    }

    private sealed class FakeTripRepository(Trip? trip) : ITripRepository
    {
        public Trip? UpdatedTrip { get; private set; }
        public int UpdateCallCount { get; private set; }

        public Task AddAsync(Trip t, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelTripCommandHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(trip);

        public Task UpdateAsync(Trip t, CancellationToken cancellationToken)
        {
            UpdateCallCount++;
            UpdatedTrip = t;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelTripCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelTripCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelTripCommandHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelTripCommandHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelTripCommandHandler.");

        public Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelTripCommandHandler.");

        public Task<bool> SlugExistsAsync(Guid organizationId, string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelTripCommandHandler.");

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelTripCommandHandler.");
    }

    private sealed class FakeReservationRepository(IReadOnlyList<Reservation> reservations) : IReservationRepository
    {
        public List<Guid> CancelledReservationIds { get; } = [];

        public Task<IReadOnlyList<Reservation>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => Task.FromResult(reservations);

        public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelTripCommandHandler.");

        public Task CreateBatchAsync(Payment payment, IReadOnlyCollection<Passenger> passengers, IReadOnlyCollection<Reservation> batchReservations, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelTripCommandHandler.");

        public Task CancelAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            CancelledReservationIds.Add(reservationId);
            var reservation = reservations.FirstOrDefault(r => r.Id == reservationId);
            if (reservation is not null)
            {
                reservation.Status = ReservationStatus.Cancelada;
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Reservation>> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelTripCommandHandler.");
    }

    private sealed class FakePaymentRepository(IReadOnlyDictionary<Guid, Payment> paymentsById) : IPaymentRepository
    {
        public List<Payment> UpdatedPayments { get; } = [];

        public Task AddAsync(Payment payment, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelTripCommandHandler.");

        public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(paymentsById.TryGetValue(id, out var payment) ? payment : null);

        public Task<Payment?> GetByExternalOrderIdAsync(string externalOrderId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelTripCommandHandler.");

        public Task UpdateAsync(Payment payment, CancellationToken cancellationToken)
        {
            UpdatedPayments.Add(payment);
            return Task.CompletedTask;
        }
    }

    private sealed record RefundCall(string ExternalOrderId, Money? Amount, string SellerAccessToken);

    private sealed class FakePaymentService(bool throwOnRefund = false) : IPaymentService
    {
        public List<RefundCall> RefundCalls { get; } = [];

        public Task<CreateOrderResult> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken, string sellerAccessToken = "")
            => throw new NotSupportedException("Not needed by CancelTripCommandHandler.");

        public Task<OrderStatusResult> GetOrderAsync(string externalOrderId, CancellationToken cancellationToken, string sellerAccessToken = "")
            => throw new NotSupportedException("Not needed by CancelTripCommandHandler.");

        public Task RefundAsync(string externalOrderId, Money? amount, CancellationToken cancellationToken, string sellerAccessToken = "")
        {
            if (throwOnRefund)
            {
                throw new InvalidOperationException("Simulated Mercado Pago refund failure.");
            }

            RefundCalls.Add(new RefundCall(externalOrderId, amount, sellerAccessToken));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMercadoPagoSellerTokenResolver(string token = "seller-access-token") : IMercadoPagoSellerTokenResolver
    {
        public List<Guid> ReceivedOrganizationIds { get; } = [];

        public Task<string> ResolveAccessTokenAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            ReceivedOrganizationIds.Add(organizationId);
            return Task.FromResult(token);
        }
    }

    private sealed class FakeRefundRepository : IRefundRepository
    {
        public List<Refund> AddedRefunds { get; } = [];

        public Task AddAsync(Refund refund, CancellationToken cancellationToken)
        {
            AddedRefunds.Add(refund);
            return Task.CompletedTask;
        }

        public Task<Refund?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelTripCommandHandler.");

        public Task<IReadOnlyList<Refund>> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelTripCommandHandler.");
    }

    private sealed class FakePassengerRepository(IReadOnlyDictionary<Guid, Passenger> passengersById) : IPassengerRepository
    {
        public Task<Passenger?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(passengersById.TryGetValue(id, out var passenger) ? passenger : null);
    }

    private sealed class FakeSeatRepository(IReadOnlyList<Seat> seats) : ISeatRepository
    {
        public Task EnsureSeatsForVehicleAsync(Guid vehicleId, int capacity, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelTripCommandHandler.");

        public Task<IReadOnlyList<Seat>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken)
            => Task.FromResult(seats);
    }

    private sealed class FakeEmailService(bool throwOnSend = false) : IEmailService
    {
        public List<EmailMessage> SentMessages { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            if (throwOnSend)
            {
                throw new InvalidOperationException("Simulated SMTP failure.");
            }

            SentMessages.Add(message);
            return Task.CompletedTask;
        }
    }

    private static Trip CreateTrip(
        DateTime departureDate, TripStatus status = TripStatus.Publicado, Guid? organizationId = null) => new()
    {
        Id = Guid.NewGuid(),
        Slug = Guid.NewGuid().ToString(),
        OrganizationId = organizationId ?? Guid.NewGuid(),
        DriverId = Guid.NewGuid(),
        VehicleId = Guid.NewGuid(),
        EventId = Guid.NewGuid(),
        DepartureDate = departureDate,
        ArrivalDate = departureDate.AddHours(4),
        Capacity = 45,
        AvailableSeats = 44,
        Price = 15000m,
        Status = status
    };

    private static Reservation CreateReservation(Guid tripId, Guid seatId, Guid passengerId, Guid? paymentId = null) => new()
    {
        Id = Guid.NewGuid(),
        TripId = tripId,
        SeatId = seatId,
        PassengerId = passengerId,
        Price = 15000m,
        PaymentId = paymentId
    };

    private static Payment CreatePayment(PaymentStatus status = PaymentStatus.Aprobado, decimal total = 15000m) => new()
    {
        Id = Guid.NewGuid(),
        ExternalOrderId = "mp-order-123",
        TotalAmount = new Money(total),
        Status = status,
        CreatedAt = DateTime.UtcNow
    };

    private static Passenger CreatePassenger(string email = "juan@checkbus.test") => new()
    {
        Id = Guid.NewGuid(),
        Name = "Juan",
        Surname = "Perez",
        DocumentType = DocumentType.DNI,
        DocumentNumber = "30111222",
        Email = email,
        Phone = "+54 11 5555-5555"
    };

    private static CancelTripCommandHandler CreateHandler(
        Trip? trip,
        IReadOnlyList<Reservation>? reservations = null,
        IReadOnlyDictionary<Guid, Passenger>? passengersById = null,
        IReadOnlyList<Seat>? seats = null,
        FakeEmailService? emailService = null,
        IReadOnlyDictionary<Guid, Payment>? paymentsById = null,
        FakePaymentService? paymentService = null,
        FakeRefundRepository? refundRepository = null,
        FakeMercadoPagoSellerTokenResolver? sellerTokenResolver = null,
        FakeTripRepository? tripRepository = null,
        FakeReservationRepository? reservationRepository = null,
        FakePaymentRepository? paymentRepository = null,
        Guid? callerOrganizationId = null)
    {
        return new CancelTripCommandHandler(
            tripRepository ?? new FakeTripRepository(trip),
            reservationRepository ?? new FakeReservationRepository(reservations ?? []),
            new FakePassengerRepository(passengersById ?? new Dictionary<Guid, Passenger>()),
            new FakeSeatRepository(seats ?? []),
            emailService ?? new FakeEmailService(),
            paymentRepository ?? new FakePaymentRepository(paymentsById ?? new Dictionary<Guid, Payment>()),
            paymentService ?? new FakePaymentService(),
            sellerTokenResolver ?? new FakeMercadoPagoSellerTokenResolver(),
            refundRepository ?? new FakeRefundRepository(),
            new FakeCurrentUserService(callerOrganizationId ?? trip?.OrganizationId ?? Guid.NewGuid()),
            NullLogger<CancelTripCommandHandler>.Instance);
    }

    private static CancelTripCommand CreateCommand(Guid tripId, TripCancellationReason reason = TripCancellationReason.ProblemaChofer)
        => new() { Id = tripId, Reason = reason };

    [Fact]
    public async Task Handle_TripNotFound_ThrowsTripNotFoundException()
    {
        var handler = CreateHandler(trip: null, callerOrganizationId: Guid.NewGuid());

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(CreateCommand(Guid.NewGuid()), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_TripBelongsToAnotherOrganization_ThrowsTripNotFoundException()
    {
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        var handler = CreateHandler(trip, callerOrganizationId: Guid.NewGuid());

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(CreateCommand(trip.Id), TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(TripStatus.EnProceso)]
    [InlineData(TripStatus.Completado)]
    [InlineData(TripStatus.Cancelado)]
    public async Task Handle_TripInNonCancellableStatus_ThrowsTripNotEligibleException(TripStatus status)
    {
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10), status);
        var tripRepository = new FakeTripRepository(trip);
        var handler = CreateHandler(trip, tripRepository: tripRepository);

        await Assert.ThrowsAsync<TripNotEligibleException>(() =>
            handler.Handle(CreateCommand(trip.Id), TestContext.Current.CancellationToken));

        Assert.Equal(0, tripRepository.UpdateCallCount);
    }

    [Fact]
    public async Task Handle_LessThan5DaysBeforeDeparture_ThrowsTripNotEligibleException()
    {
        var trip = CreateTrip(DateTime.UtcNow.AddDays(4));
        var tripRepository = new FakeTripRepository(trip);
        var handler = CreateHandler(trip, tripRepository: tripRepository);

        await Assert.ThrowsAsync<TripNotEligibleException>(() =>
            handler.Handle(CreateCommand(trip.Id), TestContext.Current.CancellationToken));

        Assert.Equal(0, tripRepository.UpdateCallCount);
    }

    [Fact]
    public async Task Handle_ExactlyAtFiveDayWindow_IsCancellable()
    {
        // >= 5 days must pass, not throw - only STRICTLY less than 5 days is blocked.
        var trip = CreateTrip(DateTime.UtcNow.AddDays(5).AddMinutes(1));
        var tripRepository = new FakeTripRepository(trip);
        var handler = CreateHandler(trip, tripRepository: tripRepository);

        await handler.Handle(CreateCommand(trip.Id), TestContext.Current.CancellationToken);

        Assert.Equal(1, tripRepository.UpdateCallCount);
        Assert.Equal(TripStatus.Cancelado, tripRepository.UpdatedTrip?.Status);
    }

    [Fact]
    public async Task Handle_HappyPath_SetsCanceladoAndCancelsEveryActiveReservation()
    {
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        var r1 = CreateReservation(trip.Id, Guid.NewGuid(), Guid.NewGuid());
        var r2 = CreateReservation(trip.Id, Guid.NewGuid(), Guid.NewGuid());
        var tripRepository = new FakeTripRepository(trip);
        var reservationRepository = new FakeReservationRepository([r1, r2]);

        var handler = CreateHandler(trip, tripRepository: tripRepository, reservationRepository: reservationRepository);

        await handler.Handle(CreateCommand(trip.Id), TestContext.Current.CancellationToken);

        Assert.Equal(TripStatus.Cancelado, tripRepository.UpdatedTrip?.Status);
        Assert.Equal(2, reservationRepository.CancelledReservationIds.Count);
        Assert.Equal(ReservationStatus.Cancelada, r1.Status);
        Assert.Equal(ReservationStatus.Cancelada, r2.Status);
    }

    [Fact]
    public async Task Handle_FamilyOfThreeUnderSamePayment_IssuesExactlyOneFullRefundForTheWholePayment()
    {
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        var payment = CreatePayment(total: 45000m);
        var r1 = CreateReservation(trip.Id, Guid.NewGuid(), Guid.NewGuid(), payment.Id);
        var r2 = CreateReservation(trip.Id, Guid.NewGuid(), Guid.NewGuid(), payment.Id);
        var r3 = CreateReservation(trip.Id, Guid.NewGuid(), Guid.NewGuid(), payment.Id);
        var reservationRepository = new FakeReservationRepository([r1, r2, r3]);
        var paymentRepository = new FakePaymentRepository(new Dictionary<Guid, Payment> { [payment.Id] = payment });
        var paymentService = new FakePaymentService();
        var refundRepository = new FakeRefundRepository();

        var handler = CreateHandler(
            trip, reservationRepository: reservationRepository, paymentRepository: paymentRepository,
            paymentService: paymentService, refundRepository: refundRepository);

        await handler.Handle(CreateCommand(trip.Id), TestContext.Current.CancellationToken);

        // Exactly ONE RefundAsync call for the whole payment, not one per reservation.
        Assert.Single(paymentService.RefundCalls);
        Assert.Equal(payment.ExternalOrderId, paymentService.RefundCalls[0].ExternalOrderId);
        Assert.Null(paymentService.RefundCalls[0].Amount); // null = full refund

        // But one Refund record per cancelled Reservation, matching IRefundRepository's convention.
        Assert.Equal(3, refundRepository.AddedRefunds.Count);
        Assert.All(refundRepository.AddedRefunds, refund => Assert.Equal(payment.Id, refund.PaymentId));

        Assert.Single(paymentRepository.UpdatedPayments);
        Assert.Equal(PaymentStatus.Reembolsado, paymentRepository.UpdatedPayments[0].Status);
    }

    [Fact]
    public async Task Handle_TwoDifferentPaymentsOnSameTrip_IssuesOneRefundPerPayment()
    {
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        var paymentA = CreatePayment();
        var paymentB = CreatePayment();
        var r1 = CreateReservation(trip.Id, Guid.NewGuid(), Guid.NewGuid(), paymentA.Id);
        var r2 = CreateReservation(trip.Id, Guid.NewGuid(), Guid.NewGuid(), paymentB.Id);
        var reservationRepository = new FakeReservationRepository([r1, r2]);
        var paymentRepository = new FakePaymentRepository(
            new Dictionary<Guid, Payment> { [paymentA.Id] = paymentA, [paymentB.Id] = paymentB });
        var paymentService = new FakePaymentService();

        var handler = CreateHandler(
            trip, reservationRepository: reservationRepository, paymentRepository: paymentRepository,
            paymentService: paymentService);

        await handler.Handle(CreateCommand(trip.Id), TestContext.Current.CancellationToken);

        Assert.Equal(2, paymentService.RefundCalls.Count);
        Assert.Equal(2, paymentRepository.UpdatedPayments.Count);
    }

    [Fact]
    public async Task Handle_PaymentStillPendiente_SkipsRefundButStillCancelsReservation()
    {
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        var payment = CreatePayment(status: PaymentStatus.Pendiente);
        var reservation = CreateReservation(trip.Id, Guid.NewGuid(), Guid.NewGuid(), payment.Id);
        var reservationRepository = new FakeReservationRepository([reservation]);
        var paymentRepository = new FakePaymentRepository(new Dictionary<Guid, Payment> { [payment.Id] = payment });
        var paymentService = new FakePaymentService();

        var handler = CreateHandler(
            trip, reservationRepository: reservationRepository, paymentRepository: paymentRepository,
            paymentService: paymentService);

        await handler.Handle(CreateCommand(trip.Id), TestContext.Current.CancellationToken);

        Assert.Equal(ReservationStatus.Cancelada, reservation.Status);
        Assert.Empty(paymentService.RefundCalls);
    }

    [Fact]
    public async Task Handle_ReservationWithNullPaymentId_SkipsRefundLogicEntirely()
    {
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        var reservation = CreateReservation(trip.Id, Guid.NewGuid(), Guid.NewGuid());
        var reservationRepository = new FakeReservationRepository([reservation]);
        var paymentService = new FakePaymentService();

        var handler = CreateHandler(trip, reservationRepository: reservationRepository, paymentService: paymentService);

        await handler.Handle(CreateCommand(trip.Id), TestContext.Current.CancellationToken);

        Assert.Equal(ReservationStatus.Cancelada, reservation.Status);
        Assert.Empty(paymentService.RefundCalls);
    }

    [Fact]
    public async Task Handle_RefundFailsForOnePayment_OtherPaymentAndTripCancellationStillSucceed()
    {
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        var payment = CreatePayment();
        var reservation = CreateReservation(trip.Id, Guid.NewGuid(), Guid.NewGuid(), payment.Id);
        var tripRepository = new FakeTripRepository(trip);
        var reservationRepository = new FakeReservationRepository([reservation]);
        var paymentRepository = new FakePaymentRepository(new Dictionary<Guid, Payment> { [payment.Id] = payment });
        var paymentService = new FakePaymentService(throwOnRefund: true);

        var handler = CreateHandler(
            trip, tripRepository: tripRepository, reservationRepository: reservationRepository,
            paymentRepository: paymentRepository, paymentService: paymentService);

        // Must not throw - the already-persisted trip/reservation cancellation must never be
        // undone by a downstream refund failure, same posture as CancelReservationCommandHandler.
        await handler.Handle(CreateCommand(trip.Id), TestContext.Current.CancellationToken);

        Assert.Equal(TripStatus.Cancelado, tripRepository.UpdatedTrip?.Status);
        Assert.Equal(ReservationStatus.Cancelada, reservation.Status);
        Assert.Empty(paymentRepository.UpdatedPayments);
    }

    [Fact]
    public async Task Handle_MultiplePassengers_SendsOneEmailEachWithCorrectSeatNumber()
    {
        var seatId1 = Guid.NewGuid();
        var seatId2 = Guid.NewGuid();
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        var passenger1 = CreatePassenger("ana@checkbus.test");
        var passenger2 = CreatePassenger("beto@checkbus.test");
        var r1 = CreateReservation(trip.Id, seatId1, passenger1.Id);
        var r2 = CreateReservation(trip.Id, seatId2, passenger2.Id);
        var seats = new List<Seat>
        {
            new() { Id = seatId1, VehicleId = trip.VehicleId, Number = 5 },
            new() { Id = seatId2, VehicleId = trip.VehicleId, Number = 12 }
        };
        var passengersById = new Dictionary<Guid, Passenger> { [passenger1.Id] = passenger1, [passenger2.Id] = passenger2 };
        var reservationRepository = new FakeReservationRepository([r1, r2]);
        var emailService = new FakeEmailService();

        var handler = CreateHandler(
            trip, reservationRepository: reservationRepository, passengersById: passengersById,
            seats: seats, emailService: emailService);

        await handler.Handle(CreateCommand(trip.Id), TestContext.Current.CancellationToken);

        Assert.Equal(2, emailService.SentMessages.Count);
        Assert.Contains(emailService.SentMessages, m => m.To == passenger1.Email && m.HtmlBody.Contains("Asiento: 5"));
        Assert.Contains(emailService.SentMessages, m => m.To == passenger2.Email && m.HtmlBody.Contains("Asiento: 12"));
    }

    [Fact]
    public async Task Handle_PassengerHasNoEmail_DoesNotAttemptToSendForThatOne()
    {
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        var passengerWithEmail = CreatePassenger("tiene-email@checkbus.test");
        var passengerWithoutEmail = CreatePassenger(email: "");
        var r1 = CreateReservation(trip.Id, Guid.NewGuid(), passengerWithEmail.Id);
        var r2 = CreateReservation(trip.Id, Guid.NewGuid(), passengerWithoutEmail.Id);
        var passengersById = new Dictionary<Guid, Passenger>
        {
            [passengerWithEmail.Id] = passengerWithEmail,
            [passengerWithoutEmail.Id] = passengerWithoutEmail
        };
        var reservationRepository = new FakeReservationRepository([r1, r2]);
        var emailService = new FakeEmailService();

        var handler = CreateHandler(
            trip, reservationRepository: reservationRepository, passengersById: passengersById, emailService: emailService);

        await handler.Handle(CreateCommand(trip.Id), TestContext.Current.CancellationToken);

        Assert.Single(emailService.SentMessages);
        Assert.Equal(passengerWithEmail.Email, emailService.SentMessages[0].To);
    }

    [Fact]
    public async Task Handle_EmailFailsForOnePassenger_OtherEmailsStillSendAndCancellationStillSucceeds()
    {
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        var passenger = CreatePassenger();
        var reservation = CreateReservation(trip.Id, Guid.NewGuid(), passenger.Id);
        var tripRepository = new FakeTripRepository(trip);
        var reservationRepository = new FakeReservationRepository([reservation]);
        var passengersById = new Dictionary<Guid, Passenger> { [passenger.Id] = passenger };
        var emailService = new FakeEmailService(throwOnSend: true);

        var handler = CreateHandler(
            trip, tripRepository: tripRepository, reservationRepository: reservationRepository,
            passengersById: passengersById, emailService: emailService);

        await handler.Handle(CreateCommand(trip.Id), TestContext.Current.CancellationToken);

        Assert.Equal(TripStatus.Cancelado, tripRepository.UpdatedTrip?.Status);
        Assert.Equal(ReservationStatus.Cancelada, reservation.Status);
        Assert.Empty(emailService.SentMessages);
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        var handler = new CancelTripCommandHandler(
            new FakeTripRepository(trip),
            new FakeReservationRepository([]),
            new FakePassengerRepository(new Dictionary<Guid, Passenger>()),
            new FakeSeatRepository([]),
            new FakeEmailService(),
            new FakePaymentRepository(new Dictionary<Guid, Payment>()),
            new FakePaymentService(),
            new FakeMercadoPagoSellerTokenResolver(),
            new FakeRefundRepository(),
            new FakeCurrentUserService(organizationId: null),
            NullLogger<CancelTripCommandHandler>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(CreateCommand(trip.Id), TestContext.Current.CancellationToken));
    }
}
