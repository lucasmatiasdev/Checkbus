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

    public class CancelReservationCommandHandler : IRequestHandler<CancelReservationCommand>
    {

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

            var trip = await _tripRepository.GetByIdAsync(reservation.TripId, cancellationToken)
                ?? throw new ReservationNotFoundException();

            var boardingRecord = await _boardingRecordRepository.GetByReservationIdAsync(
                reservation.Id, cancellationToken);

            if (boardingRecord?.Status == BoardingStatus.Abordado)
            {
                throw new ReservationNotCancellableException(
                    "Ya abordaste este viaje, no se puede cancelar la reserva.");
            }

            if (trip.DepartureDate - DateTime.UtcNow < TimeSpan.FromHours(CancellationWindowHours))
            {
                throw new ReservationNotCancellableException(
                    "Faltan menos de 24 horas para la salida de este viaje, no se puede cancelar la reserva.");
            }

            await _reservationRepository.CancelAsync(reservation.Id, cancellationToken);

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

        private async Task ProcessRefundAsync(
            Reservation reservation, Guid paymentId, decimal tripPrice, Guid organizationId, CancellationToken cancellationToken)
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken)
                ?? throw new PaymentNotFoundException();

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

            var sellerAccessToken = await _sellerTokenResolver.ResolveAccessTokenAsync(organizationId, cancellationToken);

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
