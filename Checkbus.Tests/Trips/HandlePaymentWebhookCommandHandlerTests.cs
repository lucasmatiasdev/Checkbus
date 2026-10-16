using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Payments;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Commands;
using Checkbus.ApiService.Application.Trips.Events;
using Checkbus.ApiService.Domain.Entities.Invoices;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Payments;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Payments;
using Checkbus.ApiService.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;

namespace Checkbus.Tests.Trips;

/// <summary>
/// Proves <see cref="HandlePaymentWebhookCommandHandler"/> (odd/tasks/pagos-mercadopago.md
/// Fase D4/D6): duplicate event id is a complete no-op (D3 dedupe), the approved path confirms
/// every held Reservation, creates one Invoice per Reservation and raises
/// <see cref="PaymentApprovedEvent"/>, the rejected path releases the holds immediately, and the
/// orphaned-approval path (reservations no longer PendientePago) triggers an automatic full
/// refund + explanatory email instead of reconfirmation. Fake-repository style, matching
/// <see cref="CancelReservationCommandHandlerTests"/>.
/// </summary>
public class HandlePaymentWebhookCommandHandlerTests
{
    private sealed class FakeProcessedWebhookEventRepository : IProcessedWebhookEventRepository
    {
        private readonly HashSet<string> _recorded = [];
        public int ExistsCallCount { get; private set; }
        public int RecordCallCount { get; private set; }

        public Task<bool> ExistsAsync(string externalEventId, CancellationToken cancellationToken)
        {
            ExistsCallCount++;
            return Task.FromResult(_recorded.Contains(externalEventId));
        }

        public Task RecordAsync(string externalEventId, CancellationToken cancellationToken)
        {
            RecordCallCount++;
            _recorded.Add(externalEventId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePaymentRepository(Payment? payment) : IPaymentRepository
    {
        public int UpdateCallCount { get; private set; }

        public Task AddAsync(Payment payment, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by HandlePaymentWebhookCommandHandler.");

        public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by HandlePaymentWebhookCommandHandler.");

        public Task<Payment?> GetByExternalOrderIdAsync(string externalOrderId, CancellationToken cancellationToken)
            => Task.FromResult(payment is not null && payment.ExternalOrderId == externalOrderId ? payment : null);

        public Task UpdateAsync(Payment payment, CancellationToken cancellationToken)
        {
            UpdateCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePaymentService(MercadoPagoOrderStatus status) : IPaymentService
    {
        public int RefundCallCount { get; private set; }
        public Money? LastRefundAmount { get; private set; }
        public string? LastGetOrderSellerAccessToken { get; private set; }
        public string? LastRefundSellerAccessToken { get; private set; }

        public Task<CreateOrderResult> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken, string sellerAccessToken = "")
            => throw new NotSupportedException("Not needed by HandlePaymentWebhookCommandHandler.");

        public Task<OrderStatusResult> GetOrderAsync(string externalOrderId, CancellationToken cancellationToken, string sellerAccessToken = "")
        {
            LastGetOrderSellerAccessToken = sellerAccessToken;
            return Task.FromResult(new OrderStatusResult(externalOrderId, status));
        }

        public Task RefundAsync(string externalOrderId, Money? amount, CancellationToken cancellationToken, string sellerAccessToken = "")
        {
            RefundCallCount++;
            LastRefundAmount = amount;
            LastRefundSellerAccessToken = sellerAccessToken;
            return Task.CompletedTask;
        }
    }

    // Fase I (odd/tasks/pagos-mercadopago.md, I9) — same fake-repository style as every other
    // fake in this file; only GetByIdAsync is ever exercised by this handler.
    private sealed class FakeTripRepository(Trip? trip) : ITripRepository
    {
        public Task AddAsync(Trip trip, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by HandlePaymentWebhookCommandHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(trip is not null && trip.Id == id ? trip : null);

        public Task UpdateAsync(Trip trip, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by HandlePaymentWebhookCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by HandlePaymentWebhookCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by HandlePaymentWebhookCommandHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by HandlePaymentWebhookCommandHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by HandlePaymentWebhookCommandHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by HandlePaymentWebhookCommandHandler.");

        public Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by HandlePaymentWebhookCommandHandler.");

        public Task<bool> SlugExistsAsync(Guid organizationId, string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by HandlePaymentWebhookCommandHandler.");

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by HandlePaymentWebhookCommandHandler.");
    }

    private sealed class FakeMercadoPagoSellerTokenResolver(
        string token = "seller-access-token", Exception? exception = null) : IMercadoPagoSellerTokenResolver
    {
        public Guid? ReceivedOrganizationId { get; private set; }

        public Task<string> ResolveAccessTokenAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            ReceivedOrganizationId = organizationId;

            if (exception is not null)
            {
                throw exception;
            }

            return Task.FromResult(token);
        }
    }

    private sealed class FakeReservationRepository(IReadOnlyList<Reservation> reservations) : IReservationRepository
    {
        public List<Guid> ConfirmedIds { get; } = [];
        public List<Guid> CancelledIds { get; } = [];

        public Task<IReadOnlyList<Reservation>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by HandlePaymentWebhookCommandHandler.");

        public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(reservations.FirstOrDefault(r => r.Id == id));

        public Task CreateBatchAsync(Payment payment, IReadOnlyCollection<Passenger> passengers, IReadOnlyCollection<Reservation> newReservations, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by HandlePaymentWebhookCommandHandler.");

        public Task CancelAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            CancelledIds.Add(reservationId);
            var reservation = reservations.First(r => r.Id == reservationId);
            reservation.Status = ReservationStatus.Cancelada;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Reservation>> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken)
            => Task.FromResult(reservations);

        public Task ConfirmAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            ConfirmedIds.Add(reservationId);
            var reservation = reservations.First(r => r.Id == reservationId);
            reservation.Status = ReservationStatus.Confirmada;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeInvoiceRepository : IInvoiceRepository
    {
        public List<Invoice> AddedInvoices { get; } = [];

        public Task AddAsync(Invoice invoice, CancellationToken cancellationToken)
        {
            AddedInvoices.Add(invoice);
            return Task.CompletedTask;
        }

        public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by HandlePaymentWebhookCommandHandler.");
    }

    private sealed class FakePassengerRepository(IReadOnlyList<Passenger> passengers) : IPassengerRepository
    {
        public Task<Passenger?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(passengers.FirstOrDefault(p => p.Id == id));
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

    private sealed class FakePublisher : IPublisher
    {
        public List<object> Published { get; } = [];

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            Published.Add(notification);
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            Published.Add(notification!);
            return Task.CompletedTask;
        }
    }

    private const string ExternalOrderId = "mp-order-1";
    private const string ExternalEventId = "mp-event-1";

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

    private static Reservation CreateReservation(Guid passengerId, ReservationStatus status = ReservationStatus.PendientePago) => new()
    {
        Id = Guid.NewGuid(),
        TripId = Guid.NewGuid(),
        SeatId = Guid.NewGuid(),
        PassengerId = passengerId,
        Price = 1500m,
        Status = status
    };

    private static Payment CreatePayment() => new()
    {
        Id = Guid.NewGuid(),
        ExternalOrderId = ExternalOrderId,
        TotalAmount = new Money(3000m),
        Status = PaymentStatus.Pendiente,
        CreatedAt = DateTime.UtcNow
    };

    // Fase I (odd/tasks/pagos-mercadopago.md, I9) — minimal valid Trip for a given id, used
    // only so FakeTripRepository can resolve the Organization behind a test's Reservation(s).
    private static Trip CreateTrip(Guid id) => new()
    {
        Id = id,
        Slug = Guid.NewGuid().ToString(),
        OrganizationId = Guid.NewGuid(),
        DriverId = Guid.NewGuid(),
        VehicleId = Guid.NewGuid(),
        EventId = Guid.NewGuid(),
        DepartureDate = DateTime.UtcNow.AddDays(10),
        ArrivalDate = DateTime.UtcNow.AddDays(10).AddHours(4),
        Capacity = 45,
        AvailableSeats = 45,
        Price = 1500m,
        Status = TripStatus.Publicado
    };

    [Fact]
    public async Task Handle_DuplicateEventId_IsANoOp()
    {
        var payment = CreatePayment();
        var processedEvents = new FakeProcessedWebhookEventRepository();
        await processedEvents.RecordAsync(ExternalEventId, TestContext.Current.CancellationToken);

        var paymentRepository = new FakePaymentRepository(payment);
        var paymentService = new FakePaymentService(MercadoPagoOrderStatus.Approved);
        var reservationRepository = new FakeReservationRepository([]);
        var invoiceRepository = new FakeInvoiceRepository();
        var publisher = new FakePublisher();

        var handler = CreateHandler(
            processedEvents, paymentRepository, paymentService, reservationRepository, invoiceRepository,
            new FakePassengerRepository([]), new FakeEmailService(), publisher);

        await handler.Handle(
            new HandlePaymentWebhookCommand { ExternalEventId = ExternalEventId, ExternalOrderId = ExternalOrderId },
            TestContext.Current.CancellationToken);

        // Nothing past the dedupe check ran: GetOrderAsync/UpdateAsync never touched, nothing published.
        Assert.Equal(0, paymentRepository.UpdateCallCount);
        Assert.Empty(publisher.Published);
    }

    [Fact]
    public async Task Handle_Approved_ConfirmsReservations_CreatesInvoices_AndRaisesPaymentApprovedEvent()
    {
        var passenger1 = CreatePassenger();
        var passenger2 = CreatePassenger();
        var reservation1 = CreateReservation(passenger1.Id);
        var reservation2 = CreateReservation(passenger2.Id);
        var payment = CreatePayment();

        var reservationRepository = new FakeReservationRepository([reservation1, reservation2]);
        var invoiceRepository = new FakeInvoiceRepository();
        var publisher = new FakePublisher();
        var paymentRepository = new FakePaymentRepository(payment);
        // Fase I: only reservations[0] (reservation1)'s TripId is ever looked up to resolve the
        // Organization behind this Payment — same "any one Reservation" posture the handler's
        // own doc comment describes.
        var tripRepository = new FakeTripRepository(CreateTrip(reservation1.TripId));

        var handler = CreateHandler(
            new FakeProcessedWebhookEventRepository(), paymentRepository,
            new FakePaymentService(MercadoPagoOrderStatus.Approved), reservationRepository, invoiceRepository,
            new FakePassengerRepository([passenger1, passenger2]), new FakeEmailService(), publisher,
            tripRepository);

        await handler.Handle(
            new HandlePaymentWebhookCommand { ExternalEventId = ExternalEventId, ExternalOrderId = ExternalOrderId },
            TestContext.Current.CancellationToken);

        Assert.Equal(PaymentStatus.Aprobado, payment.Status);
        Assert.Equal(ReservationStatus.Confirmada, reservation1.Status);
        Assert.Equal(ReservationStatus.Confirmada, reservation2.Status);
        Assert.Equal(2, invoiceRepository.AddedInvoices.Count);
        Assert.Contains(invoiceRepository.AddedInvoices, i => i.PassengerId == passenger1.Id && i.PaymentId == payment.Id);
        Assert.Contains(invoiceRepository.AddedInvoices, i => i.PassengerId == passenger2.Id && i.PaymentId == payment.Id);

        var raised = Assert.Single(publisher.Published);
        var approvedEvent = Assert.IsType<PaymentApprovedEvent>(raised);
        Assert.Equal(payment.Id, approvedEvent.PaymentId);
        Assert.Contains(reservation1.Id, approvedEvent.ReservationIds);
        Assert.Contains(reservation2.Id, approvedEvent.ReservationIds);
    }

    [Fact]
    public async Task Handle_Rejected_ReleasesPendientePagoHolds()
    {
        var passenger = CreatePassenger();
        var reservation = CreateReservation(passenger.Id);
        var payment = CreatePayment();

        var reservationRepository = new FakeReservationRepository([reservation]);
        var publisher = new FakePublisher();
        var tripRepository = new FakeTripRepository(CreateTrip(reservation.TripId));

        var handler = CreateHandler(
            new FakeProcessedWebhookEventRepository(), new FakePaymentRepository(payment),
            new FakePaymentService(MercadoPagoOrderStatus.Rejected), reservationRepository, new FakeInvoiceRepository(),
            new FakePassengerRepository([passenger]), new FakeEmailService(), publisher,
            tripRepository);

        await handler.Handle(
            new HandlePaymentWebhookCommand { ExternalEventId = ExternalEventId, ExternalOrderId = ExternalOrderId },
            TestContext.Current.CancellationToken);

        Assert.Equal(PaymentStatus.Rechazado, payment.Status);
        Assert.Equal(ReservationStatus.Cancelada, reservation.Status);
        Assert.Contains(reservation.Id, reservationRepository.CancelledIds);
        Assert.Empty(publisher.Published);
    }

    [Fact]
    public async Task Handle_OrphanedApproval_TriggersFullRefund_AndSendsExplanatoryEmail_NotReconfirmation()
    {
        // The hold was already released (e.g. Cancelada by some other path) before this
        // approval notification arrived — must refund, never confirm/invoice.
        var passenger = CreatePassenger();
        var reservation = CreateReservation(passenger.Id, status: ReservationStatus.Cancelada);
        var payment = CreatePayment();

        var reservationRepository = new FakeReservationRepository([reservation]);
        var invoiceRepository = new FakeInvoiceRepository();
        var publisher = new FakePublisher();
        var paymentService = new FakePaymentService(MercadoPagoOrderStatus.Approved);
        var emailService = new FakeEmailService();
        var tripRepository = new FakeTripRepository(CreateTrip(reservation.TripId));

        var handler = CreateHandler(
            new FakeProcessedWebhookEventRepository(), new FakePaymentRepository(payment),
            paymentService, reservationRepository, invoiceRepository,
            new FakePassengerRepository([passenger]), emailService, publisher,
            tripRepository);

        await handler.Handle(
            new HandlePaymentWebhookCommand { ExternalEventId = ExternalEventId, ExternalOrderId = ExternalOrderId },
            TestContext.Current.CancellationToken);

        Assert.Equal(1, paymentService.RefundCallCount);
        Assert.Null(paymentService.LastRefundAmount); // null = full refund, per IPaymentService's own convention
        Assert.Equal(PaymentStatus.Reembolsado, payment.Status);

        // Never reconfirmed, never invoiced, nothing published.
        Assert.Equal(ReservationStatus.Cancelada, reservation.Status);
        Assert.Empty(reservationRepository.ConfirmedIds);
        Assert.Empty(invoiceRepository.AddedInvoices);
        Assert.Empty(publisher.Published);

        Assert.Single(emailService.SentMessages);
        Assert.Equal(passenger.Email, emailService.SentMessages[0].To);
    }

    [Fact]
    public async Task Handle_UnknownExternalOrderId_DoesNothing_ButStillAcknowledgesDedupe()
    {
        var reservationRepository = new FakeReservationRepository([]);
        var publisher = new FakePublisher();
        var processedEvents = new FakeProcessedWebhookEventRepository();

        var handler = CreateHandler(
            processedEvents, new FakePaymentRepository(payment: null),
            new FakePaymentService(MercadoPagoOrderStatus.Approved), reservationRepository, new FakeInvoiceRepository(),
            new FakePassengerRepository([]), new FakeEmailService(), publisher);

        await handler.Handle(
            new HandlePaymentWebhookCommand { ExternalEventId = ExternalEventId, ExternalOrderId = "unknown-order" },
            TestContext.Current.CancellationToken);

        Assert.Empty(publisher.Published);
        Assert.Equal(1, processedEvents.RecordCallCount);
    }

    // --- Fase I (odd/tasks/pagos-mercadopago.md, I9): seller token resolution. ---

    [Fact]
    public async Task Handle_Approved_ResolvesSellerToken_AndPassesItThroughToGetOrderAsync()
    {
        var passenger = CreatePassenger();
        var reservation = CreateReservation(passenger.Id);
        var payment = CreatePayment();
        var trip = CreateTrip(reservation.TripId);

        var reservationRepository = new FakeReservationRepository([reservation]);
        var paymentService = new FakePaymentService(MercadoPagoOrderStatus.Approved);
        var sellerTokenResolver = new FakeMercadoPagoSellerTokenResolver(token: "org-specific-token");

        var handler = CreateHandler(
            new FakeProcessedWebhookEventRepository(), new FakePaymentRepository(payment),
            paymentService, reservationRepository, new FakeInvoiceRepository(),
            new FakePassengerRepository([passenger]), new FakeEmailService(), new FakePublisher(),
            new FakeTripRepository(trip), sellerTokenResolver);

        await handler.Handle(
            new HandlePaymentWebhookCommand { ExternalEventId = ExternalEventId, ExternalOrderId = ExternalOrderId },
            TestContext.Current.CancellationToken);

        Assert.Equal(trip.OrganizationId, sellerTokenResolver.ReceivedOrganizationId);
        Assert.Equal("org-specific-token", paymentService.LastGetOrderSellerAccessToken);
    }

    [Fact]
    public async Task Handle_OrphanedApproval_PassesSellerTokenThroughToRefundAsync()
    {
        var passenger = CreatePassenger();
        var reservation = CreateReservation(passenger.Id, status: ReservationStatus.Cancelada);
        var payment = CreatePayment();
        var trip = CreateTrip(reservation.TripId);

        var reservationRepository = new FakeReservationRepository([reservation]);
        var paymentService = new FakePaymentService(MercadoPagoOrderStatus.Approved);
        var sellerTokenResolver = new FakeMercadoPagoSellerTokenResolver(token: "org-specific-token");

        var handler = CreateHandler(
            new FakeProcessedWebhookEventRepository(), new FakePaymentRepository(payment),
            paymentService, reservationRepository, new FakeInvoiceRepository(),
            new FakePassengerRepository([passenger]), new FakeEmailService(), new FakePublisher(),
            new FakeTripRepository(trip), sellerTokenResolver);

        await handler.Handle(
            new HandlePaymentWebhookCommand { ExternalEventId = ExternalEventId, ExternalOrderId = ExternalOrderId },
            TestContext.Current.CancellationToken);

        Assert.Equal("org-specific-token", paymentService.LastRefundSellerAccessToken);
    }

    [Fact]
    public async Task Handle_OrganizationNotConnected_PropagatesWithoutUpdatingPayment()
    {
        var passenger = CreatePassenger();
        var reservation = CreateReservation(passenger.Id);
        var payment = CreatePayment();
        var trip = CreateTrip(reservation.TripId);

        var reservationRepository = new FakeReservationRepository([reservation]);
        var paymentRepository = new FakePaymentRepository(payment);
        var sellerTokenResolver = new FakeMercadoPagoSellerTokenResolver(
            exception: new OrganizationNotConnectedToMercadoPagoException());

        var handler = CreateHandler(
            new FakeProcessedWebhookEventRepository(), paymentRepository,
            new FakePaymentService(MercadoPagoOrderStatus.Approved), reservationRepository, new FakeInvoiceRepository(),
            new FakePassengerRepository([passenger]), new FakeEmailService(), new FakePublisher(),
            new FakeTripRepository(trip), sellerTokenResolver);

        await Assert.ThrowsAsync<OrganizationNotConnectedToMercadoPagoException>(() =>
            handler.Handle(
                new HandlePaymentWebhookCommand { ExternalEventId = ExternalEventId, ExternalOrderId = ExternalOrderId },
                TestContext.Current.CancellationToken));

        // GetOrderAsync/UpdateAsync never ran — the exception propagated before either.
        Assert.Equal(0, paymentRepository.UpdateCallCount);
    }

    private static HandlePaymentWebhookCommandHandler CreateHandler(
        FakeProcessedWebhookEventRepository processedEvents,
        FakePaymentRepository paymentRepository,
        FakePaymentService paymentService,
        FakeReservationRepository reservationRepository,
        FakeInvoiceRepository invoiceRepository,
        FakePassengerRepository passengerRepository,
        FakeEmailService emailService,
        FakePublisher publisher,
        FakeTripRepository? tripRepository = null,
        FakeMercadoPagoSellerTokenResolver? sellerTokenResolver = null)
    {
        return new HandlePaymentWebhookCommandHandler(
            processedEvents,
            paymentRepository,
            paymentService,
            reservationRepository,
            tripRepository ?? new FakeTripRepository(null),
            sellerTokenResolver ?? new FakeMercadoPagoSellerTokenResolver(),
            invoiceRepository,
            passengerRepository,
            emailService,
            publisher,
            NullLogger<HandlePaymentWebhookCommandHandler>.Instance);
    }
}
