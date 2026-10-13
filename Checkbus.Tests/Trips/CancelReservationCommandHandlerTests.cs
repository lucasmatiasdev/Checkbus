using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Payments;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Commands;
using Checkbus.ApiService.Domain.Entities.BoardingRecords;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Payments;
using Checkbus.ApiService.Domain.Entities.Refunds;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Entities.Seats;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Reservations;
using Checkbus.ApiService.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

namespace Checkbus.Tests.Trips;

/// <summary>
/// Proves <see cref="CancelReservationCommandHandler"/> (odd/tasks/reservas-publicas.md
/// Fase 4 / C2, C4, C6): IDOR-safe load-by-id shape with NO ownership check (public surface,
/// no caller identity), the two independent cancellation-blocking reasons (Abordado /
/// inside-24h), delegation of the actual Status flip to
/// <see cref="IReservationRepository.CancelAsync"/>, and the best-effort cancellation email
/// (sent after success, never propagates a failure). BoardingRecord itself must never be
/// touched by cancellation. Fake-repository style, matching
/// <see cref="ValidateBoardingCommandHandlerTests"/>/<c>CreatePublicReservationCommandHandlerTests</c>.
/// </summary>
public class CancelReservationCommandHandlerTests
{
    private sealed class FakeReservationRepository(Reservation? reservation, IReadOnlyList<Reservation>? paymentSiblings = null) : IReservationRepository
    {
        public Guid? CancelledReservationId { get; private set; }

        public Task<IReadOnlyList<Reservation>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(reservation);

        public Task CreateBatchAsync(Payment payment, IReadOnlyCollection<Passenger> passengers, IReadOnlyCollection<Reservation> reservations, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task CancelAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            CancelledReservationId = reservationId;
            if (reservation is not null)
            {
                reservation.Status = ReservationStatus.Cancelada;
            }

            return Task.CompletedTask;
        }

        // Fase F1: resolves this Payment's other Reservations so the handler can decide
        // partial- vs. full-refund. Defaults to empty (no active siblings -> full refund) when
        // a test doesn't care about the Payment-grouping scenario at all.
        public Task<IReadOnlyList<Reservation>> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken)
            => Task.FromResult(paymentSiblings ?? (IReadOnlyList<Reservation>)[]);
    }

    private sealed class FakePaymentRepository(Payment? payment) : IPaymentRepository
    {
        public int UpdateCallCount { get; private set; }
        public Payment? UpdatedPayment { get; private set; }

        public Task AddAsync(Payment payment, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(payment is not null && payment.Id == id ? payment : null);

        public Task<Payment?> GetByExternalOrderIdAsync(string externalOrderId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task UpdateAsync(Payment payment, CancellationToken cancellationToken)
        {
            UpdateCallCount++;
            UpdatedPayment = payment;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePaymentService(bool throwOnRefund = false) : IPaymentService
    {
        public int RefundCallCount { get; private set; }
        public string? LastExternalOrderId { get; private set; }
        public Money? LastRefundAmount { get; private set; }

        public Task<CreateOrderResult> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken, string sellerAccessToken = "")
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task<OrderStatusResult> GetOrderAsync(string externalOrderId, CancellationToken cancellationToken, string sellerAccessToken = "")
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task RefundAsync(string externalOrderId, Money? amount, CancellationToken cancellationToken, string sellerAccessToken = "")
        {
            if (throwOnRefund)
            {
                throw new InvalidOperationException("Simulated Mercado Pago refund failure.");
            }

            RefundCallCount++;
            LastExternalOrderId = externalOrderId;
            LastRefundAmount = amount;
            return Task.CompletedTask;
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
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task<IReadOnlyList<Refund>> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");
    }

    private sealed class FakeTripRepository(Trip? trip) : ITripRepository
    {
        public Task AddAsync(Trip trip, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(trip);

        public Task UpdateAsync(Trip trip, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");
    }

    private sealed class FakeBoardingRecordRepository(BoardingRecord? record) : IBoardingRecordRepository
    {
        public Task EnsureForTripAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task<IReadOnlyList<BoardingRecord>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task<BoardingRecord?> GetByReservationIdAsync(Guid reservationId, CancellationToken cancellationToken)
            => Task.FromResult(record);

        public Task ValidateAsync(Guid reservationId, ValidationMethod method, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

        public Task CloseBoardingAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");
    }

    private sealed class FakePassengerRepository(Passenger? passenger) : IPassengerRepository
    {
        public Task<Passenger?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(passenger);
    }

    private sealed class FakeSeatRepository(IReadOnlyList<Seat> seats) : ISeatRepository
    {
        public Task EnsureSeatsForVehicleAsync(Guid vehicleId, int capacity, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CancelReservationCommandHandler.");

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

    private static Trip CreateTrip(DateTime departureDate) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        DriverId = Guid.NewGuid(),
        VehicleId = Guid.NewGuid(),
        EventId = Guid.NewGuid(),
        DepartureDate = departureDate,
        ArrivalDate = departureDate.AddHours(4),
        Capacity = 45,
        AvailableSeats = 44,
        Price = 15000m,
        Status = TripStatus.Publicado
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

    private static Payment CreatePayment(PaymentStatus status = PaymentStatus.Aprobado) => new()
    {
        Id = Guid.NewGuid(),
        ExternalOrderId = "mp-order-123",
        TotalAmount = new Money(45000m),
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

    private static CancelReservationCommandHandler CreateHandler(
        FakeReservationRepository reservationRepository,
        Trip? trip,
        BoardingRecord? boardingRecord = null,
        Passenger? passenger = null,
        IReadOnlyList<Seat>? seats = null,
        FakeEmailService? emailService = null,
        FakePaymentRepository? paymentRepository = null,
        FakePaymentService? paymentService = null,
        FakeRefundRepository? refundRepository = null)
    {
        return new CancelReservationCommandHandler(
            reservationRepository,
            new FakeTripRepository(trip),
            new FakeBoardingRecordRepository(boardingRecord),
            new FakePassengerRepository(passenger),
            new FakeSeatRepository(seats ?? []),
            emailService ?? new FakeEmailService(),
            paymentRepository ?? new FakePaymentRepository(null),
            paymentService ?? new FakePaymentService(),
            refundRepository ?? new FakeRefundRepository(),
            NullLogger<CancelReservationCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ReservationNotFound_ThrowsReservationNotFound()
    {
        var handler = CreateHandler(new FakeReservationRepository(null), null);

        await Assert.ThrowsAsync<ReservationNotFoundException>(() =>
            handler.Handle(
                new CancelReservationCommand { ReservationId = Guid.NewGuid() },
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_BoardingRecordAlreadyAbordado_ThrowsReservationNotCancellable()
    {
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        var reservation = CreateReservation(trip.Id, Guid.NewGuid(), Guid.NewGuid());
        var boardingRecord = new BoardingRecord { Id = Guid.NewGuid(), ReservationId = reservation.Id, Status = BoardingStatus.Abordado };
        var reservationRepository = new FakeReservationRepository(reservation);

        var handler = CreateHandler(reservationRepository, trip, boardingRecord);

        await Assert.ThrowsAsync<ReservationNotCancellableException>(() =>
            handler.Handle(
                new CancelReservationCommand { ReservationId = reservation.Id },
                TestContext.Current.CancellationToken));

        Assert.Null(reservationRepository.CancelledReservationId);
    }

    [Fact]
    public async Task Handle_LessThan24HoursBeforeDeparture_ThrowsReservationNotCancellable()
    {
        var trip = CreateTrip(DateTime.UtcNow.AddHours(5));
        var reservation = CreateReservation(trip.Id, Guid.NewGuid(), Guid.NewGuid());
        var boardingRecord = new BoardingRecord { Id = Guid.NewGuid(), ReservationId = reservation.Id, Status = BoardingStatus.Pendiente };
        var reservationRepository = new FakeReservationRepository(reservation);

        var handler = CreateHandler(reservationRepository, trip, boardingRecord);

        await Assert.ThrowsAsync<ReservationNotCancellableException>(() =>
            handler.Handle(
                new CancelReservationCommand { ReservationId = reservation.Id },
                TestContext.Current.CancellationToken));

        Assert.Null(reservationRepository.CancelledReservationId);
    }

    [Fact]
    public async Task Handle_HappyPath_CancelsReservation_AndLeavesPendienteBoardingRecordUntouched()
    {
        var seatId = Guid.NewGuid();
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        var reservation = CreateReservation(trip.Id, seatId, Guid.NewGuid());
        var boardingRecord = new BoardingRecord { Id = Guid.NewGuid(), ReservationId = reservation.Id, Status = BoardingStatus.Pendiente };
        var seats = new List<Seat> { new() { Id = seatId, VehicleId = trip.VehicleId, Number = 7 } };
        var reservationRepository = new FakeReservationRepository(reservation);

        var handler = CreateHandler(reservationRepository, trip, boardingRecord, passenger: null, seats);

        await handler.Handle(
            new CancelReservationCommand { ReservationId = reservation.Id },
            TestContext.Current.CancellationToken);

        Assert.Equal(reservation.Id, reservationRepository.CancelledReservationId);
        Assert.Equal(ReservationStatus.Cancelada, reservation.Status);

        // Cancellation must NEVER touch BoardingRecord — not deleted, not altered.
        Assert.Equal(BoardingStatus.Pendiente, boardingRecord.Status);
    }

    [Fact]
    public async Task Handle_AusenteBoardingRecord_DoesNotBlockCancellation()
    {
        // Only Abordado blocks cancellation — Pendiente/Ausente do not.
        var seatId = Guid.NewGuid();
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        var reservation = CreateReservation(trip.Id, seatId, Guid.NewGuid());
        var boardingRecord = new BoardingRecord { Id = Guid.NewGuid(), ReservationId = reservation.Id, Status = BoardingStatus.Ausente };
        var reservationRepository = new FakeReservationRepository(reservation);

        var handler = CreateHandler(reservationRepository, trip, boardingRecord);

        await handler.Handle(
            new CancelReservationCommand { ReservationId = reservation.Id },
            TestContext.Current.CancellationToken);

        Assert.Equal(reservation.Id, reservationRepository.CancelledReservationId);
        Assert.Equal(BoardingStatus.Ausente, boardingRecord.Status);
    }

    [Fact]
    public async Task Handle_HappyPath_WithPassengerEmail_SendsCancellationEmail()
    {
        var seatId = Guid.NewGuid();
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        var reservation = CreateReservation(trip.Id, seatId, Guid.NewGuid());
        var passenger = CreatePassenger();
        var seats = new List<Seat> { new() { Id = seatId, VehicleId = trip.VehicleId, Number = 7 } };
        var emailService = new FakeEmailService();
        var reservationRepository = new FakeReservationRepository(reservation);

        var handler = CreateHandler(reservationRepository, trip, boardingRecord: null, passenger, seats, emailService);

        await handler.Handle(
            new CancelReservationCommand { ReservationId = reservation.Id },
            TestContext.Current.CancellationToken);

        Assert.Single(emailService.SentMessages);
        Assert.Equal(passenger.Email, emailService.SentMessages[0].To);
    }

    [Fact]
    public async Task Handle_EmailServiceThrows_DoesNotPropagate_CancellationStillSucceeds()
    {
        var seatId = Guid.NewGuid();
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        var reservation = CreateReservation(trip.Id, seatId, Guid.NewGuid());
        var passenger = CreatePassenger();
        var seats = new List<Seat> { new() { Id = seatId, VehicleId = trip.VehicleId, Number = 7 } };
        var emailService = new FakeEmailService(throwOnSend: true);
        var reservationRepository = new FakeReservationRepository(reservation);

        var handler = CreateHandler(reservationRepository, trip, boardingRecord: null, passenger, seats, emailService);

        // Must not throw — the email failure is logged and swallowed, the already-persisted
        // cancellation is unaffected.
        await handler.Handle(
            new CancelReservationCommand { ReservationId = reservation.Id },
            TestContext.Current.CancellationToken);

        Assert.Equal(reservation.Id, reservationRepository.CancelledReservationId);
        Assert.Empty(emailService.SentMessages);
    }

    [Fact]
    public async Task Handle_PassengerHasNoEmail_DoesNotAttemptToSend()
    {
        var seatId = Guid.NewGuid();
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        var reservation = CreateReservation(trip.Id, seatId, Guid.NewGuid());
        var passenger = CreatePassenger(email: "");
        var seats = new List<Seat> { new() { Id = seatId, VehicleId = trip.VehicleId, Number = 7 } };
        var emailService = new FakeEmailService();
        var reservationRepository = new FakeReservationRepository(reservation);

        var handler = CreateHandler(reservationRepository, trip, boardingRecord: null, passenger, seats, emailService);

        await handler.Handle(
            new CancelReservationCommand { ReservationId = reservation.Id },
            TestContext.Current.CancellationToken);

        Assert.Empty(emailService.SentMessages);
    }

    // --- Fase F (odd/tasks/pagos-mercadopago.md): cancellation triggers a real refund. ---

    [Fact]
    public async Task Handle_OneOfThreeActiveSeatsOnSamePayment_IssuesPartialRefundForThatSeatPriceOnly()
    {
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        var payment = CreatePayment();
        var reservation = CreateReservation(trip.Id, Guid.NewGuid(), Guid.NewGuid(), payment.Id);
        var sibling1 = CreateReservation(trip.Id, Guid.NewGuid(), Guid.NewGuid(), payment.Id);
        sibling1.Status = ReservationStatus.Confirmada;
        var sibling2 = CreateReservation(trip.Id, Guid.NewGuid(), Guid.NewGuid(), payment.Id);
        sibling2.Status = ReservationStatus.PendientePago;

        var reservationRepository = new FakeReservationRepository(reservation, [sibling1, sibling2]);
        var paymentRepository = new FakePaymentRepository(payment);
        var paymentService = new FakePaymentService();
        var refundRepository = new FakeRefundRepository();

        var handler = CreateHandler(
            reservationRepository, trip,
            paymentRepository: paymentRepository, paymentService: paymentService, refundRepository: refundRepository);

        await handler.Handle(
            new CancelReservationCommand { ReservationId = reservation.Id },
            TestContext.Current.CancellationToken);

        Assert.Equal(ReservationStatus.Cancelada, reservation.Status);
        Assert.Equal(1, paymentService.RefundCallCount);
        Assert.Equal(payment.ExternalOrderId, paymentService.LastExternalOrderId);
        Assert.Equal(new Money(trip.Price), paymentService.LastRefundAmount);

        Assert.Single(refundRepository.AddedRefunds);
        var refund = refundRepository.AddedRefunds[0];
        Assert.Equal(payment.Id, refund.PaymentId);
        Assert.Equal(reservation.Id, refund.ReservationId);
        Assert.Equal(new Money(trip.Price), refund.Amount);

        Assert.Equal(1, paymentRepository.UpdateCallCount);
        Assert.Equal(PaymentStatus.ParcialmenteReembolsado, paymentRepository.UpdatedPayment?.Status);
    }

    [Fact]
    public async Task Handle_LastActiveSeatOnPayment_IssuesFullRefund()
    {
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        var payment = CreatePayment();
        var reservation = CreateReservation(trip.Id, Guid.NewGuid(), Guid.NewGuid(), payment.Id);
        // Only sibling on this Payment is already Cancelada — this is the last active seat.
        var cancelledSibling = CreateReservation(trip.Id, Guid.NewGuid(), Guid.NewGuid(), payment.Id);
        cancelledSibling.Status = ReservationStatus.Cancelada;

        var reservationRepository = new FakeReservationRepository(reservation, [cancelledSibling]);
        var paymentRepository = new FakePaymentRepository(payment);
        var paymentService = new FakePaymentService();
        var refundRepository = new FakeRefundRepository();

        var handler = CreateHandler(
            reservationRepository, trip,
            paymentRepository: paymentRepository, paymentService: paymentService, refundRepository: refundRepository);

        await handler.Handle(
            new CancelReservationCommand { ReservationId = reservation.Id },
            TestContext.Current.CancellationToken);

        Assert.Equal(1, paymentService.RefundCallCount);
        Assert.Null(paymentService.LastRefundAmount);
        Assert.Equal(PaymentStatus.Reembolsado, paymentRepository.UpdatedPayment?.Status);
        Assert.Single(refundRepository.AddedRefunds);
    }

    [Fact]
    public async Task Handle_ReservationWithNullPaymentId_SkipsRefundLogicEntirely()
    {
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        // PaymentId stays null (pre-payment-feature Reservation, or any edge case where it's unset).
        var reservation = CreateReservation(trip.Id, Guid.NewGuid(), Guid.NewGuid());
        var reservationRepository = new FakeReservationRepository(reservation);
        var paymentRepository = new FakePaymentRepository(null);
        var paymentService = new FakePaymentService();
        var refundRepository = new FakeRefundRepository();

        var handler = CreateHandler(
            reservationRepository, trip,
            paymentRepository: paymentRepository, paymentService: paymentService, refundRepository: refundRepository);

        await handler.Handle(
            new CancelReservationCommand { ReservationId = reservation.Id },
            TestContext.Current.CancellationToken);

        // Existing cancellation flow still runs unchanged.
        Assert.Equal(reservation.Id, reservationRepository.CancelledReservationId);
        Assert.Equal(ReservationStatus.Cancelada, reservation.Status);

        // No refund machinery is ever touched.
        Assert.Equal(0, paymentService.RefundCallCount);
        Assert.Equal(0, paymentRepository.UpdateCallCount);
        Assert.Empty(refundRepository.AddedRefunds);
    }

    [Fact]
    public async Task Handle_RefundServiceThrows_ReservationStillCancelled()
    {
        var trip = CreateTrip(DateTime.UtcNow.AddDays(10));
        var payment = CreatePayment();
        var reservation = CreateReservation(trip.Id, Guid.NewGuid(), Guid.NewGuid(), payment.Id);
        var reservationRepository = new FakeReservationRepository(reservation);
        var paymentRepository = new FakePaymentRepository(payment);
        var paymentService = new FakePaymentService(throwOnRefund: true);
        var refundRepository = new FakeRefundRepository();

        var handler = CreateHandler(
            reservationRepository, trip,
            paymentRepository: paymentRepository, paymentService: paymentService, refundRepository: refundRepository);

        // Must not throw — the refund failure is logged and swallowed, same posture as the
        // existing email-failure test above; the already-persisted cancellation is unaffected.
        await handler.Handle(
            new CancelReservationCommand { ReservationId = reservation.Id },
            TestContext.Current.CancellationToken);

        Assert.Equal(reservation.Id, reservationRepository.CancelledReservationId);
        Assert.Equal(ReservationStatus.Cancelada, reservation.Status);

        // The failed call never got to persist anything downstream of it.
        Assert.Empty(refundRepository.AddedRefunds);
        Assert.Equal(0, paymentRepository.UpdateCallCount);
    }
}
