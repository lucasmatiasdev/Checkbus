using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Payments;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Refunds;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Payments;
using Checkbus.ApiService.Domain.Exceptions.Reservations;
using Checkbus.ApiService.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    // Public, unauthenticated cancellation (odd/tasks/reservas-publicas.md Fase 4 / C2). Same
    // IDOR-safe shape as ValidateBoardingCommandHandler (load by id -> not-found if null ->
    // business-rule checks -> delegate the actual mutation to the repository), but with NO
    // ownership/auth check at all — unlike the Chofer-facing boarding surface, this public
    // surface has no authenticated caller to check ownership against; Reservation.Id alone is
    // the access control (same reasoning every other public handler in this feature uses).
    //
    // Two independent rejection reasons, either one alone is enough to block cancellation:
    //   1. The Reservation's BoardingRecord is already Abordado (mirrors A4's "no
    //      re-validating" rejection posture — BoardingRecord itself is NEVER touched here).
    //   2. Fewer than CancellationWindowHours remain before Trip.DepartureDate.
    // Both map to the same ReservationNotCancellableException type (409), with a contextual
    // Spanish message built per call site that is shown verbatim to the passenger.
    public class CancelReservationCommandHandler : IRequestHandler<CancelReservationCommand>
    {
        // Hardcoded, not external config — same reasoning Fase 1 R6 used for the 20/min
        // rate-limit number: no measured traffic/business data yet to justify making this
        // configurable.
        public const int CancellationWindowHours = 24;

        private readonly IReservationRepository _reservationRepository;
        private readonly ITripRepository _tripRepository;
        private readonly IBoardingRecordRepository _boardingRecordRepository;
        private readonly IPassengerRepository _passengerRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly IEmailService _emailService;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPaymentService _paymentService;
        private readonly IMercadoPagoSellerTokenResolver _sellerTokenResolver;
        private readonly IRefundRepository _refundRepository;
        private readonly ILogger<CancelReservationCommandHandler> _logger;

        public CancelReservationCommandHandler(
            IReservationRepository reservationRepository,
            ITripRepository tripRepository,
            IBoardingRecordRepository boardingRecordRepository,
            IPassengerRepository passengerRepository,
            ISeatRepository seatRepository,
            IEmailService emailService,
            IPaymentRepository paymentRepository,
            IPaymentService paymentService,
            IMercadoPagoSellerTokenResolver sellerTokenResolver,
            IRefundRepository refundRepository,
            ILogger<CancelReservationCommandHandler> logger)
        {
            _reservationRepository = reservationRepository;
            _tripRepository = tripRepository;
            _boardingRecordRepository = boardingRecordRepository;
            _passengerRepository = passengerRepository;
            _seatRepository = seatRepository;
            _emailService = emailService;
            _paymentRepository = paymentRepository;
            _paymentService = paymentService;
            _sellerTokenResolver = sellerTokenResolver;
            _refundRepository = refundRepository;
            _logger = logger;
        }

        public async Task Handle(CancelReservationCommand request, CancellationToken cancellationToken)
        {
            var reservation = await _reservationRepository.GetByIdAsync(request.ReservationId, cancellationToken)
                ?? throw new ReservationNotFoundException();

            // Should not happen given FK integrity, but a Reservation whose Trip row is
            // missing cannot have its cancellation window checked — same "doesn't exist" IDOR
            // framing as every other *NotFoundException in this codebase.
            var trip = await _tripRepository.GetByIdAsync(reservation.TripId, cancellationToken)
                ?? throw new ReservationNotFoundException();

            var boardingRecord = await _boardingRecordRepository.GetByReservationIdAsync(
                reservation.Id, cancellationToken);

            if (boardingRecord?.Status == BoardingStatus.Abordado)
            {
                throw new ReservationNotCancellableException(
                    "Ya abordaste este viaje, no se puede cancelar la reserva.");
            }

            // DepartureDate is always stored as UTC (CreateTripCommandHandler normalizes it
            // via DateTime.SpecifyKind(..., DateTimeKind.Utc) on creation) — safe to compare
            // directly against DateTime.UtcNow.
            if (trip.DepartureDate - DateTime.UtcNow < TimeSpan.FromHours(CancellationWindowHours))
            {
                throw new ReservationNotCancellableException(
                    "Faltan menos de 24 horas para la salida de este viaje, no se puede cancelar la reserva.");
            }

            await _reservationRepository.CancelAsync(reservation.Id, cancellationToken);

            // Fase F (odd/tasks/pagos-mercadopago.md): the seat is now durably freed. Give the
            // money back via Mercado Pago if there was ever a charge for it — a Reservation
            // created before this feature shipped (or any edge case where it's unset) has
            // PaymentId == null, meaning no Mercado Pago order was ever created for it, so
            // there is nothing to refund; skip straight to the existing email step below.
            // Wrapped in its own try/catch, separate from the email's: the Reservation is
            // already durably cancelled at this point, so a flaky downstream Mercado Pago call
            // must never turn into a failed cancellation response for the passenger — same
            // posture this handler already takes with the confirmation email.
            if (reservation.PaymentId is not null)
            {
                try
                {
                    await ProcessRefundAsync(
                        reservation, reservation.PaymentId.Value, trip.Price, trip.OrganizationId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to process Mercado Pago refund for ReservationId {ReservationId}",
                        reservation.Id);
                }
            }

            // Sending happens after the cancellation is durably persisted: it is already real
            // at this point, so a flaky SMTP relay must never turn into a failed response for
            // the passenger (same posture M2's CreatePublicReservationCommandHandler uses for
            // the original confirmation email).
            var passenger = await _passengerRepository.GetByIdAsync(reservation.PassengerId, cancellationToken);
            if (passenger is null || string.IsNullOrWhiteSpace(passenger.Email))
            {
                return;
            }

            try
            {
                var seats = await _seatRepository.GetByVehicleIdAsync(trip.VehicleId, cancellationToken);
                var seatNumber = seats.FirstOrDefault(s => s.Id == reservation.SeatId)?.Number;
                var message = BuildCancellationEmail(trip.DepartureDate, passenger.Name, passenger.Email, seatNumber);
                await _emailService.SendAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to send reservation cancellation email for ReservationId {ReservationId}",
                    reservation.Id);
            }
        }

        // Fase F1: partial if this Payment still covers other active seats, full if this was
        // the last one. "Active" means still PendientePago or Confirmada — Cancelada siblings
        // (including this one, already flipped by CancelAsync above) don't count. Refund amount
        // comes from Trip.Price (the handler already loaded Trip for the 24h-window check
        // above, reused here rather than re-fetching or reading Reservation.Price — both are
        // the same value at creation time, see Reservation's own doc comment, but Trip.Price is
        // the source of truth this Fase was asked to use).
        private async Task ProcessRefundAsync(
            Reservation reservation, Guid paymentId, decimal tripPrice, Guid organizationId, CancellationToken cancellationToken)
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken)
                ?? throw new PaymentNotFoundException();

            // Defensive, not a business rule the passenger can trigger: a Payment reachable from
            // a Reservation's own PaymentId FK should always be Aprobado/ParcialmenteReembolsado
            // by the time someone cancels a Confirmada seat. Guards against attempting a refund
            // on a Payment that was never actually charged (Pendiente) or has nothing left to
            // give back (already fully Reembolsado) — PaymentNotRefundableException exists
            // specifically for this (see its own doc comment).
            if (payment.Status is PaymentStatus.Pendiente or PaymentStatus.Reembolsado)
            {
                throw new PaymentNotRefundableException();
            }

            var externalOrderId = payment.ExternalOrderId
                ?? throw new PaymentNotRefundableException("Payment has no ExternalOrderId to refund.");

            var siblings = await _reservationRepository.GetByPaymentIdAsync(paymentId, cancellationToken);
            var hasActiveSiblings = siblings.Any(sibling =>
                sibling.Id != reservation.Id
                && (sibling.Status == ReservationStatus.PendientePago || sibling.Status == ReservationStatus.Confirmada));

            var refundAmount = new Money(tripPrice);

            // Fase I (Marketplace/Split Payments): resolve THIS Trip's own Organization's
            // Mercado Pago seller token before refunding — real money must be given back from
            // that Organization's own account, not Checkbus's. Resolving/throwing here is
            // covered by this method's own caller's try/catch (Handle() above): a flaky/failed
            // resolution must never turn into a failed cancellation response for the passenger,
            // same posture as a flaky RefundAsync call itself.
            var sellerAccessToken = await _sellerTokenResolver.ResolveAccessTokenAsync(organizationId, cancellationToken);

            // IPaymentService.RefundAsync returns Task (void) — Fase B's port has no return
            // value to carry a gateway-issued refund id back, so Refund.ExternalRefundId stays
            // null here (see Refund's own doc comment: the field is nullable for exactly this).
            await _paymentService.RefundAsync(
                externalOrderId, hasActiveSiblings ? refundAmount : null, cancellationToken, sellerAccessToken);

            await _refundRepository.AddAsync(new Refund
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                ReservationId = reservation.Id,
                Amount = refundAmount,
                ExternalRefundId = null,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            payment.Status = hasActiveSiblings
                ? PaymentStatus.ParcialmenteReembolsado
                : PaymentStatus.Reembolsado;
            await _paymentRepository.UpdateAsync(payment, cancellationToken);
        }

        private static EmailMessage BuildCancellationEmail(
            DateTime departureDate, string passengerName, string passengerEmail, int? seatNumber)
        {
            const string subject = "Tu reserva fue cancelada";
            var seatLine = seatNumber is null ? string.Empty : $"<li>Asiento: {seatNumber}</li>";
            var body = $"""
                <h2>Reserva cancelada</h2>
                <p>Hola {passengerName},</p>
                <p>Tu reserva fue cancelada correctamente.</p>
                <ul>
                    <li>Fecha de salida: {departureDate:dd/MM/yyyy HH:mm}</li>
                    {seatLine}
                </ul>
                """;

            return new EmailMessage(passengerEmail, subject, body);
        }
    }
}
