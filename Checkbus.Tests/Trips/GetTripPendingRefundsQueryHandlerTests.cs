using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Queries;
using Checkbus.ApiService.Domain.Entities.Payments;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using Checkbus.ApiService.Domain.ValueObjects;

namespace Checkbus.Tests.Trips;

public class GetTripPendingRefundsQueryHandlerTests
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
        public Task AddAsync(Trip t, Route route, IEnumerable<Stop> stops, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripPendingRefundsQueryHandler.");

        public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(trip);

        public Task UpdateAsync(Trip t, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripPendingRefundsQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripPendingRefundsQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByVehicleIdAsync(Guid vehicleId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripPendingRefundsQueryHandler.");

        public Task<IReadOnlyList<Trip>> GetOverlappingByDriverIdAsync(Guid driverId, DateTime departureDate, DateTime arrivalDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripPendingRefundsQueryHandler.");

        public Task<Route?> GetRouteByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripPendingRefundsQueryHandler.");

        public Task<IReadOnlyList<Stop>> GetStopsByRouteIdAsync(Guid routeId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripPendingRefundsQueryHandler.");

        public Task UpdateStopDepartureTimesAsync(
            IReadOnlyDictionary<Guid, DateTime> departureTimesByStopId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripPendingRefundsQueryHandler.");

        public Task<bool> SlugExistsAsync(Guid organizationId, string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripPendingRefundsQueryHandler.");

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripPendingRefundsQueryHandler.");
    }

    private sealed class FakeReservationRepository(IReadOnlyList<Reservation> reservations) : IReservationRepository
    {
        public Task<IReadOnlyList<Reservation>> GetByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripPendingRefundsQueryHandler.");

        public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripPendingRefundsQueryHandler.");

        public Task CreateBatchAsync(Payment payment, IReadOnlyCollection<Checkbus.ApiService.Domain.Entities.Passengers.Passenger> passengers, IReadOnlyCollection<Reservation> batchReservations, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripPendingRefundsQueryHandler.");

        public Task CancelAsync(Guid reservationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripPendingRefundsQueryHandler.");

        public Task<IReadOnlyList<Reservation>> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripPendingRefundsQueryHandler.");

        public Task<IReadOnlyList<Reservation>> GetAllByTripIdAsync(Guid tripId, CancellationToken cancellationToken)
            => Task.FromResult(reservations);
    }

    private sealed class FakePaymentRepository(IReadOnlyDictionary<Guid, Payment> paymentsById) : IPaymentRepository
    {
        public Task AddAsync(Payment payment, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripPendingRefundsQueryHandler.");

        public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(paymentsById.TryGetValue(id, out var payment) ? payment : null);

        public Task<Payment?> GetByExternalOrderIdAsync(string externalOrderId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripPendingRefundsQueryHandler.");

        public Task UpdateAsync(Payment payment, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetTripPendingRefundsQueryHandler.");
    }

    private static Trip CreateTrip(Guid? organizationId = null) => new()
    {
        Id = Guid.NewGuid(),
        Slug = Guid.NewGuid().ToString(),
        OrganizationId = organizationId ?? Guid.NewGuid(),
        DriverId = Guid.NewGuid(),
        VehicleId = Guid.NewGuid(),
        EventId = Guid.NewGuid(),
        DepartureDate = DateTime.UtcNow.AddDays(10),
        ArrivalDate = DateTime.UtcNow.AddDays(10).AddHours(4),
        Capacity = 45,
        AvailableSeats = 45,
        Price = 15000m,
        Status = TripStatus.Cancelado
    };

    private static Reservation CreateReservation(
        Guid tripId, ReservationStatus status = ReservationStatus.Cancelada, Guid? paymentId = null) => new()
    {
        Id = Guid.NewGuid(),
        TripId = tripId,
        SeatId = Guid.NewGuid(),
        PassengerId = Guid.NewGuid(),
        Price = 15000m,
        Status = status,
        PaymentId = paymentId
    };

    private static Payment CreatePayment(PaymentStatus status, decimal total = 15000m) => new()
    {
        Id = Guid.NewGuid(),
        ExternalOrderId = "mp-order-123",
        TotalAmount = new Money(total),
        Status = status,
        CreatedAt = DateTime.UtcNow
    };

    private static GetTripPendingRefundsQueryHandler CreateHandler(
        Trip? trip,
        IReadOnlyList<Reservation>? reservations = null,
        IReadOnlyDictionary<Guid, Payment>? paymentsById = null,
        Guid? callerOrganizationId = null)
    {
        return new GetTripPendingRefundsQueryHandler(
            new FakeTripRepository(trip),
            new FakeReservationRepository(reservations ?? []),
            new FakePaymentRepository(paymentsById ?? new Dictionary<Guid, Payment>()),
            new FakeCurrentUserService(callerOrganizationId ?? trip?.OrganizationId ?? Guid.NewGuid()));
    }

    [Fact]
    public async Task Handle_TripNotFound_ThrowsTripNotFoundException()
    {
        var handler = CreateHandler(trip: null, callerOrganizationId: Guid.NewGuid());

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(new GetTripPendingRefundsQuery { TripId = Guid.NewGuid() }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_TripBelongsToAnotherOrganization_ThrowsTripNotFoundException()
    {
        var trip = CreateTrip();
        var handler = CreateHandler(trip, callerOrganizationId: Guid.NewGuid());

        await Assert.ThrowsAsync<TripNotFoundException>(() =>
            handler.Handle(new GetTripPendingRefundsQuery { TripId = trip.Id }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_CancelledReservationWithApprovedUnrefundedPayment_ReturnsIt()
    {
        var trip = CreateTrip();
        var payment = CreatePayment(PaymentStatus.Aprobado);
        var reservation = CreateReservation(trip.Id, ReservationStatus.Cancelada, payment.Id);
        var handler = CreateHandler(
            trip, [reservation], new Dictionary<Guid, Payment> { [payment.Id] = payment });

        var result = await handler.Handle(
            new GetTripPendingRefundsQuery { TripId = trip.Id }, TestContext.Current.CancellationToken);

        var pending = Assert.Single(result);
        Assert.Equal(payment.Id, pending.PaymentId);
        Assert.Equal(payment.ExternalOrderId, pending.ExternalOrderId);
        Assert.Equal(payment.TotalAmount.Amount, pending.Amount);
        Assert.Equal([reservation.Id], pending.ReservationIds);
    }

    [Fact]
    public async Task Handle_CancelledReservationWithPartiallyRefundedPayment_ReturnsIt()
    {
        var trip = CreateTrip();
        var payment = CreatePayment(PaymentStatus.ParcialmenteReembolsado);
        var reservation = CreateReservation(trip.Id, ReservationStatus.Cancelada, payment.Id);
        var handler = CreateHandler(
            trip, [reservation], new Dictionary<Guid, Payment> { [payment.Id] = payment });

        var result = await handler.Handle(
            new GetTripPendingRefundsQuery { TripId = trip.Id }, TestContext.Current.CancellationToken);

        Assert.Single(result);
    }

    [Fact]
    public async Task Handle_FullyRefundedPayment_IsExcluded()
    {
        var trip = CreateTrip();
        var payment = CreatePayment(PaymentStatus.Reembolsado);
        var reservation = CreateReservation(trip.Id, ReservationStatus.Cancelada, payment.Id);
        var handler = CreateHandler(
            trip, [reservation], new Dictionary<Guid, Payment> { [payment.Id] = payment });

        var result = await handler.Handle(
            new GetTripPendingRefundsQuery { TripId = trip.Id }, TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_PendingPaymentNeverCharged_IsExcluded()
    {
        var trip = CreateTrip();
        var payment = CreatePayment(PaymentStatus.Pendiente);
        var reservation = CreateReservation(trip.Id, ReservationStatus.Cancelada, payment.Id);
        var handler = CreateHandler(
            trip, [reservation], new Dictionary<Guid, Payment> { [payment.Id] = payment });

        var result = await handler.Handle(
            new GetTripPendingRefundsQuery { TripId = trip.Id }, TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_ReservationWithoutPayment_IsExcluded()
    {
        var trip = CreateTrip();
        var reservation = CreateReservation(trip.Id, ReservationStatus.Cancelada, paymentId: null);
        var handler = CreateHandler(trip, [reservation]);

        var result = await handler.Handle(
            new GetTripPendingRefundsQuery { TripId = trip.Id }, TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_FamilyOfThreeUnderSamePayment_ReturnsOneEntryGroupingAllReservationIds()
    {
        var trip = CreateTrip();
        var payment = CreatePayment(PaymentStatus.Aprobado, total: 45000m);
        var r1 = CreateReservation(trip.Id, ReservationStatus.Cancelada, payment.Id);
        var r2 = CreateReservation(trip.Id, ReservationStatus.Cancelada, payment.Id);
        var r3 = CreateReservation(trip.Id, ReservationStatus.Cancelada, payment.Id);
        var handler = CreateHandler(
            trip, [r1, r2, r3], new Dictionary<Guid, Payment> { [payment.Id] = payment });

        var result = await handler.Handle(
            new GetTripPendingRefundsQuery { TripId = trip.Id }, TestContext.Current.CancellationToken);

        var pending = Assert.Single(result);
        Assert.Equal(3, pending.ReservationIds.Count);
        Assert.Contains(r1.Id, pending.ReservationIds);
        Assert.Contains(r2.Id, pending.ReservationIds);
        Assert.Contains(r3.Id, pending.ReservationIds);
    }

    [Fact]
    public async Task Handle_NonCancelledReservation_IsIgnoredEvenIfPaymentIsApproved()
    {
        var trip = CreateTrip();
        var payment = CreatePayment(PaymentStatus.Aprobado);
        var reservation = CreateReservation(trip.Id, ReservationStatus.Confirmada, payment.Id);
        var handler = CreateHandler(
            trip, [reservation], new Dictionary<Guid, Payment> { [payment.Id] = payment });

        var result = await handler.Handle(
            new GetTripPendingRefundsQuery { TripId = trip.Id }, TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var trip = CreateTrip();
        var handler = new GetTripPendingRefundsQueryHandler(
            new FakeTripRepository(trip),
            new FakeReservationRepository([]),
            new FakePaymentRepository(new Dictionary<Guid, Payment>()),
            new FakeCurrentUserService(organizationId: null));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new GetTripPendingRefundsQuery { TripId = trip.Id }, TestContext.Current.CancellationToken));
    }
}
