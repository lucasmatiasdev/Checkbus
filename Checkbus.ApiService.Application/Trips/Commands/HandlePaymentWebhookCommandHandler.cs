using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Payments;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Events;
using Checkbus.ApiService.Domain.Entities.Invoices;
using Checkbus.ApiService.Domain.Entities.Payments;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Checkbus.ApiService.Application.Trips.Commands
{

    public class HandlePaymentWebhookCommandHandler : IRequestHandler<HandlePaymentWebhookCommand>
    {
        private readonly IProcessedWebhookEventRepository _processedWebhookEventRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPaymentService _paymentService;
        private readonly IReservationRepository _reservationRepository;
        private readonly ITripRepository _tripRepository;
        private readonly IMercadoPagoSellerTokenResolver _sellerTokenResolver;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IPassengerRepository _passengerRepository;
        private readonly IEmailService _emailService;
        private readonly IPublisher _publisher;
        private readonly ILogger<HandlePaymentWebhookCommandHandler> _logger;

        public HandlePaymentWebhookCommandHandler(
            IProcessedWebhookEventRepository processedWebhookEventRepository,
            IPaymentRepository paymentRepository,
            IPaymentService paymentService,
            IReservationRepository reservationRepository,
            ITripRepository tripRepository,
            IMercadoPagoSellerTokenResolver sellerTokenResolver,
            IInvoiceRepository invoiceRepository,
            IPassengerRepository passengerRepository,
            IEmailService emailService,
            IPublisher publisher,
            ILogger<HandlePaymentWebhookCommandHandler> logger)
        {
            _processedWebhookEventRepository = processedWebhookEventRepository;
            _paymentRepository = paymentRepository;
            _paymentService = paymentService;
            _reservationRepository = reservationRepository;
            _tripRepository = tripRepository;
            _sellerTokenResolver = sellerTokenResolver;
            _invoiceRepository = invoiceRepository;
            _passengerRepository = passengerRepository;
            _emailService = emailService;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task Handle(HandlePaymentWebhookCommand request, CancellationToken cancellationToken)
        {
            if (await _processedWebhookEventRepository.ExistsAsync(request.ExternalEventId, cancellationToken))
            {

                return;
            }

            await _processedWebhookEventRepository.RecordAsync(request.ExternalEventId, cancellationToken);

            var payment = await _paymentRepository.GetByExternalOrderIdAsync(request.ExternalOrderId, cancellationToken);
            if (payment is null)
            {
                _logger.LogWarning(
                    "Webhook notification for unknown Mercado Pago order {ExternalOrderId}.", request.ExternalOrderId);
                return;
            }

            var reservationsForToken = await _reservationRepository.GetByPaymentIdAsync(payment.Id, cancellationToken);
            if (reservationsForToken.Count == 0)
            {
                _logger.LogWarning(
                    "No Reservations linked to Payment {PaymentId}; cannot resolve an Organization for its Mercado Pago seller token.",
                    payment.Id);
                return;
            }

            var trip = await _tripRepository.GetByIdAsync(reservationsForToken[0].TripId, cancellationToken);
            if (trip is null)
            {
                _logger.LogWarning(
                    "Trip {TripId} referenced by Payment {PaymentId}'s Reservations was not found; cannot resolve its Organization's Mercado Pago seller token.",
                    reservationsForToken[0].TripId, payment.Id);
                return;
            }

            var sellerAccessToken = await _sellerTokenResolver.ResolveAccessTokenAsync(trip.OrganizationId, cancellationToken);

            var orderStatus = await _paymentService.GetOrderAsync(request.ExternalOrderId, cancellationToken, sellerAccessToken);
            var mappedStatus = MapStatus(orderStatus.Status);
            if (mappedStatus is null)
            {
                _logger.LogWarning(
                    "Mercado Pago order {ExternalOrderId} returned an unmapped status ({Status}); Payment {PaymentId} left unchanged.",
                    request.ExternalOrderId, orderStatus.Status, payment.Id);
                return;
            }

            payment.Status = mappedStatus.Value;
            await _paymentRepository.UpdateAsync(payment, cancellationToken);

            switch (mappedStatus.Value)
            {
                case PaymentStatus.Aprobado:
                    await HandleApprovedAsync(payment, request.ExternalOrderId, sellerAccessToken, cancellationToken);
                    break;

                case PaymentStatus.Rechazado:
                    await HandleRejectedAsync(payment.Id, cancellationToken);
                    break;

                default:

                    break;
            }
        }

        private async Task HandleApprovedAsync(
            Payment payment, string externalOrderId, string sellerAccessToken, CancellationToken cancellationToken)
        {
            var reservations = await _reservationRepository.GetByPaymentIdAsync(payment.Id, cancellationToken);

            var allStillHeld = reservations.Count > 0
                && reservations.All(r => r.Status == ReservationStatus.PendientePago);

            if (!allStillHeld)
            {
                await HandleOrphanedApprovalAsync(payment, externalOrderId, reservations, sellerAccessToken, cancellationToken);
                return;
            }

            var confirmedReservationIds = new List<Guid>(reservations.Count);

            foreach (var reservation in reservations)
            {
                await _reservationRepository.ConfirmAsync(reservation.Id, cancellationToken);

                await _invoiceRepository.AddAsync(new Invoice
                {
                    Id = Guid.NewGuid(),
                    TripId = reservation.TripId,
                    PassengerId = reservation.PassengerId,
                    PaymentId = payment.Id,
                    IssuedAt = DateTime.UtcNow
                }, cancellationToken);

                confirmedReservationIds.Add(reservation.Id);
            }

            await _publisher.Publish(
                new PaymentApprovedEvent { PaymentId = payment.Id, ReservationIds = confirmedReservationIds },
                cancellationToken);
        }

        private async Task HandleOrphanedApprovalAsync(
            Payment payment, string externalOrderId, IReadOnlyList<Reservation> reservations,
            string sellerAccessToken, CancellationToken cancellationToken)
        {
            await _paymentService.RefundAsync(externalOrderId, amount: null, cancellationToken, sellerAccessToken);

            payment.Status = PaymentStatus.Reembolsado;
            await _paymentRepository.UpdateAsync(payment, cancellationToken);

            foreach (var reservation in reservations)
            {
                var passenger = await _passengerRepository.GetByIdAsync(reservation.PassengerId, cancellationToken);
                if (passenger is null || string.IsNullOrWhiteSpace(passenger.Email))
                {
                    continue;
                }

                try
                {
                    var message = BuildOrphanedRefundEmail(passenger.Name, passenger.Email, payment.TotalAmount.Amount);
                    await _emailService.SendAsync(message, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex, "Failed to send orphaned-refund email for Payment {PaymentId}", payment.Id);
                }
            }
        }

        private async Task HandleRejectedAsync(Guid paymentId, CancellationToken cancellationToken)
        {
            var reservations = await _reservationRepository.GetByPaymentIdAsync(paymentId, cancellationToken);

            foreach (var reservation in reservations.Where(r => r.Status == ReservationStatus.PendientePago))
            {
                await _reservationRepository.CancelAsync(reservation.Id, cancellationToken);
            }
        }

        private static PaymentStatus? MapStatus(MercadoPagoOrderStatus status) => status switch
        {
            MercadoPagoOrderStatus.Approved => PaymentStatus.Aprobado,
            MercadoPagoOrderStatus.Rejected => PaymentStatus.Rechazado,
            MercadoPagoOrderStatus.Refunded => PaymentStatus.Reembolsado,
            MercadoPagoOrderStatus.Pending => PaymentStatus.Pendiente,
            _ => null
        };

        private static EmailMessage BuildOrphanedRefundEmail(string passengerName, string passengerEmail, decimal refundedAmount)
        {
            const string subject = "No pudimos confirmar tu reserva — ya procesamos tu reembolso";
            var body = $"""
                <h2>Tu pago fue reembolsado</h2>
                <p>Hola {passengerName},</p>
                <p>Lamentablemente no pudimos confirmar tu reserva porque el lugar ya no estaba disponible.</p>
                <p>Ya procesamos el reembolso completo de {refundedAmount:C} a tu medio de pago original.</p>
                """;

            return new EmailMessage(passengerEmail, subject, body);
        }
    }
}
