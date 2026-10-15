using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Payments;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Refunds;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Payments;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using Checkbus.ApiService.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    // Admin-initiated full-trip cancellation (CU-05) — a different action from the public,
    // passenger-initiated CancelReservationCommand (one seat), though it reuses that handler's
    // refund/email shape as its base. Cancels every still-active Reservation on the Trip,
    // refunds every covering Payment, and notifies every affected passenger.
    public class CancelTripCommandHandler : IRequestHandler<CancelTripCommand>
    {
        // Business policy (2026-10-15): passengers must have at least this many days' notice
        // to find an alternative, so a trip can never be cancelled closer to its departure than
        // this window — no override, unlike CancelReservationCommandHandler's 24h window which
        // is about seat availability, not passenger notice.
        public const int MinimumCancellationNoticeDays = 5;

        private readonly ITripRepository _tripRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly IPassengerRepository _passengerRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly IEmailService _emailService;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPaymentService _paymentService;
        private readonly IMercadoPagoSellerTokenResolver _sellerTokenResolver;
        private readonly IRefundRepository _refundRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<CancelTripCommandHandler> _logger;

        public CancelTripCommandHandler(
            ITripRepository tripRepository,
            IReservationRepository reservationRepository,
            IPassengerRepository passengerRepository,
            ISeatRepository seatRepository,
            IEmailService emailService,
            IPaymentRepository paymentRepository,
            IPaymentService paymentService,
            IMercadoPagoSellerTokenResolver sellerTokenResolver,
            IRefundRepository refundRepository,
            ICurrentUserService currentUser,
            ILogger<CancelTripCommandHandler> logger)
        {
            _tripRepository = tripRepository;
            _reservationRepository = reservationRepository;
            _passengerRepository = passengerRepository;
            _seatRepository = seatRepository;
            _emailService = emailService;
            _paymentRepository = paymentRepository;
            _paymentService = paymentService;
            _sellerTokenResolver = sellerTokenResolver;
            _refundRepository = refundRepository;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task Handle(CancelTripCommand request, CancellationToken cancellationToken)
        {
            // Planificador/Administrador-only is enforced at the controller; this handler only
            // verifies tenant ownership of the target trip, same split as PublishTripCommandHandler.
            var callerOrganizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var trip = await _tripRepository.GetByIdAsync(request.Id, cancellationToken);
            if (trip is null || trip.OrganizationId != callerOrganizationId)
            {
                // IDOR-safe: cross-tenant existence is never leaked to the caller.
                throw new TripNotFoundException();
            }

            if (trip.Status is not (TripStatus.Programado or TripStatus.Publicado))
            {
                throw new TripNotEligibleException(
                    "El viaje no puede cancelarse: solo un viaje en estado Programado o Publicado puede cancelarse.");
            }

            // DepartureDate is always stored as UTC (CreateTripCommandHandler normalizes it via
            // DateTime.SpecifyKind(..., DateTimeKind.Utc) on creation) — safe to compare
            // directly against DateTime.UtcNow.
            if (trip.DepartureDate - DateTime.UtcNow < TimeSpan.FromDays(MinimumCancellationNoticeDays))
            {
                throw new TripNotEligibleException(
                    $"Faltan menos de {MinimumCancellationNoticeDays} días para la salida de este viaje, no se puede cancelar.");
            }

            // Cancelado is excluded from ITripRepository's overlap queries (see
            // TripRepository.GetOverlappingByVehicleIdAsync/GetOverlappingByDriverIdAsync) — this
            // single status flip is what actually frees the driver/vehicle for a new trip in the
            // same window, there is no separate "unassign" step since DriverId/VehicleId are
            // required, non-nullable FKs.
            trip.Status = TripStatus.Cancelado;
            trip.UpdatedAt = DateTime.UtcNow;
            await _tripRepository.UpdateAsync(trip, cancellationToken);

            // Excludes Cancelada rows already (see IReservationRepository.GetByTripIdAsync's own
            // doc comment) — covers both Confirmada (paid) and PendientePago (in-flight,
            // unconfirmed) seats, so an in-flight checkout never survives a cancelled trip.
            var reservations = await _reservationRepository.GetByTripIdAsync(trip.Id, cancellationToken);

            foreach (var reservation in reservations)
            {
                await _reservationRepository.CancelAsync(reservation.Id, cancellationToken);
            }

            // Refund happens after every Reservation is durably cancelled: the cancellation is
            // already real at this point, so a flaky downstream Mercado Pago call must never
            // turn a successful cancellation into a failed response — same posture
            // CancelReservationCommandHandler uses for its own refund step.
            //
            // Grouped by Payment, never by Reservation: a Payment can cover several seats bought
            // together (e.g. a family of 3), and cancelling the whole trip cancels every seat
            // under it at once — there is never a still-active "sibling" seat left to justify a
            // partial refund, unlike CancelReservationCommandHandler. Every refund here is a
            // full refund of the Payment's own TotalAmount via a single RefundAsync(amount: null)
            // call per Payment, not one call per Reservation (which would double/triple-refund a
            // multi-seat Payment).
            var paymentIds = reservations
                .Where(r => r.PaymentId is not null)
                .Select(r => r.PaymentId!.Value)
                .Distinct();

            foreach (var paymentId in paymentIds)
            {
                try
                {
                    await ProcessRefundAsync(paymentId, reservations, trip.OrganizationId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to process Mercado Pago refund for PaymentId {PaymentId} while cancelling TripId {TripId}",
                        paymentId, trip.Id);
                }
            }

            // Fetched once, outside the per-passenger loop below — GetByVehicleIdAsync's result
            // never changes across reservations on the same Trip, so repeating the call once per
            // passenger (like CancelReservationCommandHandler does, which only ever cancels one
            // reservation at a time) would be a pure N+1 for a multi-seat trip.
            var seats = await _seatRepository.GetByVehicleIdAsync(trip.VehicleId, cancellationToken);
            var seatNumbersBySeatId = seats.ToDictionary(s => s.Id, s => s.Number);

            foreach (var reservation in reservations)
            {
                try
                {
                    await SendCancellationEmailAsync(reservation, trip.DepartureDate, seatNumbersBySeatId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to send trip cancellation email for ReservationId {ReservationId}",
                        reservation.Id);
                }
            }
        }

        // Adapted from CancelReservationCommandHandler.ProcessRefundAsync: same guard posture
        // (never refund a Payment that was never actually charged or already fully refunded),
        // but always a full refund of the whole Payment — see this method's caller for why.
        private async Task ProcessRefundAsync(
            Guid paymentId, IReadOnlyList<Reservation> tripReservations, Guid organizationId, CancellationToken cancellationToken)
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken)
                ?? throw new PaymentNotFoundException();

            // Defensive, same reasoning as CancelReservationCommandHandler: a Payment reachable
            // from a cancelled Reservation's own PaymentId FK is normally Aprobado, but a
            // PendientePago seat whose checkout never completed has a Payment that is still
            // Pendiente — nothing was ever charged, so there is nothing to refund.
            if (payment.Status is PaymentStatus.Pendiente or PaymentStatus.Reembolsado)
            {
                return;
            }

            var externalOrderId = payment.ExternalOrderId
                ?? throw new PaymentNotRefundableException("Payment has no ExternalOrderId to refund.");

            // Fase I (Marketplace/Split Payments): resolve THIS Trip's own Organization's
            // Mercado Pago seller token before refunding — real money must be given back from
            // that Organization's own account, not Checkbus's.
            var sellerAccessToken = await _sellerTokenResolver.ResolveAccessTokenAsync(organizationId, cancellationToken);

            await _paymentService.RefundAsync(externalOrderId, amount: null, cancellationToken, sellerAccessToken);

            foreach (var reservation in tripReservations.Where(r => r.PaymentId == paymentId))
            {
                await _refundRepository.AddAsync(new Refund
                {
                    Id = Guid.NewGuid(),
                    PaymentId = payment.Id,
                    ReservationId = reservation.Id,
                    Amount = new Money(reservation.Price),
                    ExternalRefundId = null,
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);
            }

            payment.Status = PaymentStatus.Reembolsado;
            await _paymentRepository.UpdateAsync(payment, cancellationToken);
        }

        private async Task SendCancellationEmailAsync(
            Reservation reservation,
            DateTime departureDate,
            IReadOnlyDictionary<Guid, int> seatNumbersBySeatId,
            CancellationToken cancellationToken)
        {
            var passenger = await _passengerRepository.GetByIdAsync(reservation.PassengerId, cancellationToken);
            if (passenger is null || string.IsNullOrWhiteSpace(passenger.Email))
            {
                return;
            }

            seatNumbersBySeatId.TryGetValue(reservation.SeatId, out var seatNumber);
            var message = BuildCancellationEmail(departureDate, passenger.Name, passenger.Email, seatNumber);
            await _emailService.SendAsync(message, cancellationToken);
        }

        private static EmailMessage BuildCancellationEmail(
            DateTime departureDate, string passengerName, string passengerEmail, int seatNumber)
        {
            const string subject = "Tu viaje fue cancelado";
            var body = $"""
                <h2>Viaje cancelado</h2>
                <p>Hola {passengerName},</p>
                <p>Lamentablemente el viaje que reservaste fue cancelado. Tu reserva fue anulada
                   y, si correspondía un pago, el reembolso ya fue procesado.</p>
                <ul>
                    <li>Fecha de salida: {departureDate:dd/MM/yyyy HH:mm}</li>
                    <li>Asiento: {seatNumber}</li>
                </ul>
                """;

            return new EmailMessage(passengerEmail, subject, body);
        }
    }
}
