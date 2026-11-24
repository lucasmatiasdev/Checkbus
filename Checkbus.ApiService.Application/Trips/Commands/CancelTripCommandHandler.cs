using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Payments;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Audit;
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

    public class CancelTripCommandHandler : IRequestHandler<CancelTripCommand>
    {

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
        private readonly IAuditLogRepository _auditLogRepository;
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
            IAuditLogRepository auditLogRepository,
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
            _auditLogRepository = auditLogRepository;
            _logger = logger;
        }

        public async Task Handle(CancelTripCommand request, CancellationToken cancellationToken)
        {

            var callerOrganizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var trip = await _tripRepository.GetByIdAsync(request.Id, cancellationToken);
            if (trip is null || trip.OrganizationId != callerOrganizationId)
            {

                throw new TripNotFoundException();
            }

            if (trip.Status is not (TripStatus.Programado or TripStatus.Publicado))
            {
                throw new TripNotEligibleException(
                    "El viaje no puede cancelarse: solo un viaje en estado Programado o Publicado puede cancelarse.");
            }

            if (trip.DepartureDate - DateTime.UtcNow < TimeSpan.FromDays(MinimumCancellationNoticeDays))
            {
                throw new TripNotEligibleException(
                    $"Faltan menos de {MinimumCancellationNoticeDays} días para la salida de este viaje, no se puede cancelar.");
            }

            trip.Status = TripStatus.Cancelado;
            trip.UpdatedAt = DateTime.UtcNow;
            await _tripRepository.UpdateAsync(trip, cancellationToken);

            try
            {
                await _auditLogRepository.AddAsync(new AuditLogEntry
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = trip.OrganizationId,
                    ActorUserId = _currentUser.UserId,
                    ActorEmail = _currentUser.Email ?? string.Empty,
                    Action = AuditAction.ViajeCancelado,
                    EntityType = "Trip",
                    EntityId = trip.Id,
                    Description = "Viaje cancelado",
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write audit log entry for {Action}", AuditAction.ViajeCancelado);
            }

            var reservations = await _reservationRepository.GetByTripIdAsync(trip.Id, cancellationToken);

            foreach (var reservation in reservations)
            {
                await _reservationRepository.CancelAsync(reservation.Id, cancellationToken);
            }

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

        private async Task ProcessRefundAsync(
            Guid paymentId, IReadOnlyList<Reservation> tripReservations, Guid organizationId, CancellationToken cancellationToken)
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken)
                ?? throw new PaymentNotFoundException();

            if (payment.Status is PaymentStatus.Pendiente or PaymentStatus.Reembolsado)
            {
                return;
            }

            var externalOrderId = payment.ExternalOrderId
                ?? throw new PaymentNotRefundableException("Payment has no ExternalOrderId to refund.");

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
